# Module messaging

Modules use Rebus directly through `IModuleBus<TModule>`. Register the queue and its typed bus together:

```csharp
services.AddHeroMessaging(configuration, "identity");
services.AddModuleBus<IdentityModule>("identity");
services.AddQueueHandler<UserRegisteredHandler>("identity");
```

Only one handler is allowed per `(queue, message type)`. Both registration and registry construction reject duplicates, including repeated registrations of the same handler. Queue matching is case insensitive. Different queues may handle the same event independently.

The host uses `Messaging.Rebus` and `Messaging.Rebus.Configuration`; the older `Web.MessageBus` classes are retained for compatibility and are not used by the migrated registrations.

## Configuration

The messaging connection comes from `RebusOptions:Database:ConnectionString`, falling back to the existing `DatabaseOptions:ConnectionString`. All configured module buses share one messaging data source and inbox. Do not register module-specific databases through this API.

```json
{
  "RebusOptions": {
    "NumberOfWorkers": 2,
    "MaxParallelism": 10,
    "RabbitMq": { "ConnectionString": "amqp://localhost" },
    "Storage": {
      "OutboxTableName": "rebus_outbox",
      "TimeoutsTableName": "rebus_timeout"
    },
    "Inbox": {
      "Enabled": true,
      "SchemaName": "messaging",
      "TableName": "inbox_messages",
      "LeaseDuration": "00:03:00",
      "RenewalInterval": "00:00:45",
      "BusyRetryDelay": "00:00:30",
      "Retention": "30.00:00:00",
      "CleanupInterval": "01:00:00",
      "CleanupBatchSize": 1000
    }
  }
}
```

The inbox hosted service creates its schema/table before Rebus starts. The application database principal needs permission to create these objects, or they must be provisioned ahead of time. Cleanup removes only old `Processed` records in bounded batches. Choose retention longer than the period in which messages may be replayed; after cleanup, an old message can run again. Failed records are retained for investigation.

## Incoming inbox

The incoming step uses one shared table keyed by `(queue_name, message_id)`. Rebus message IDs must be globally unique across tenants. Preserve the ID when retrying the same message; use a new ID for a new business operation.

Claims are atomic PostgreSQL upserts. `Processing` records have an expiring lease; a heartbeat renews it while processing and committing. Every renewal/completion/failure checks the owner's random lock ID. Expired owners cannot renew or mark the message processed. A busy delivery is deferred through Rebus' transport-message API, retaining its ID and body, before the current delivery is acknowledged. A failure to defer throws and is retried normally.

Handler failures release the lease and propagate to Rebus' retry/error-queue machinery. Process death is recovered by lease expiry. The step marks `Processed` only after downstream outbox persistence succeeds, in the broker transaction's commit callbacks, before ACK.

The shared inbox uses its own database operations. **It is not atomic with business data or the outgoing outbox.** A crash between business/outbox commit and inbox completion can run the handler again. Handlers must use business keys/unique constraints or another idempotent operation. HTTP, email, jobs and webhook receivers require their own idempotency strategy. Lease loss blocks inbox completion but cannot undo a handler already running.

The incoming outbox step buffers outgoing messages in its own PostgreSQL transaction and flushes them before committing. It replaces the built-in `Rebus.PostgreSql.Outbox.OutboxIncomingStep` by type name for the pinned Rebus.PostgreSql 9.1.1 version, avoiding its commit-before-flush ordering. The integration test exercises this pipeline; rerun it when upgrading Rebus. Business DbContexts are not automatically enlisted in this transaction.

## Outgoing transactions

For a standalone operation:

```csharp
await bus.Publish(new UserRegistered(userId));
```

Outside a Rebus transaction, `OutboxBus` creates, flushes and commits a separate PostgreSQL transaction for each call. Inside an active Rebus scope it joins that scope. `Advanced` exposes the native Rebus API directly; it does not automatically create an external transaction for advanced API calls.

For business data and outgoing messages to commit together, borrow the business transaction explicitly:

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(ct);
using (var messaging = bus.UseTransaction(
    (NpgsqlConnection)db.Database.GetDbConnection(),
    (NpgsqlTransaction)transaction.GetDbTransaction()))
{
    db.Users.Add(user);
    await bus.Publish(new UserRegistered(user.Id));
    await db.SaveChangesAsync(ct);
    await messaging.CompleteAsync(); // Flush messages BEFORE the database commit.
}
await transaction.CommitAsync(ct);
```

`UseTransaction` is synchronous so the Rebus ambient scope flows into the caller. `CompleteAsync` only flushes messages into the supplied transaction. The caller owns commit, rollback and disposal of both connection and transaction. If any step fails, roll back/dispose the business transaction. Never commit it before `CompleteAsync`. Nested explicit module scopes are rejected; concurrent operations must each own their scope/connection. The singleton bus wrapper never stores transaction state.

This works inside handlers as well: the explicit scope restores the incoming Rebus context on disposal, preserving incoming headers and reply support. The business database must contain the configured Rebus outbox table, and the configured dispatcher must read that same table in that same database. `IModuleBus.UseTransaction` rejects a different host, port or database. Passing a transaction from a separate tenant database without an outbox dispatcher there is unsupported. A central inbox cannot make a transaction atomic across different databases.

## Verification

```powershell
dotnet test src/Tests/Messaging.Rebus.Tests/Messaging.Rebus.Tests.csproj
# To include PostgreSQL integration tests, set MESSAGING_TEST_CONNECTION first.
```

Integration tests use unique `messaging_tests_*` schemas and remove only those schemas afterward. They use the Rebus in-memory transport, so no RabbitMQ instance is needed. Without the environment variable, PostgreSQL tests are reported as skipped.
