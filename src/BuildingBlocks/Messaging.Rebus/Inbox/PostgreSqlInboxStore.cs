using Npgsql;

namespace Messaging.Rebus.Inbox;

public sealed class PostgreSqlInboxStore : IInboxStore
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly InboxOptions _options;
    private readonly string _table;

    public PostgreSqlInboxStore(NpgsqlDataSource dataSource, InboxOptions options)
    {
        options.Validate();
        _dataSource = dataSource;
        _options = options;
        _table = $"\"{options.SchemaName}\".\"{options.TableName}\"";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource.CreateCommand($"""
            CREATE SCHEMA IF NOT EXISTS "{_options.SchemaName}";
            CREATE TABLE IF NOT EXISTS {_table} (
                queue_name text NOT NULL,
                message_id text NOT NULL,
                status text NOT NULL CHECK (status IN ('Processing', 'Processed', 'Failed')),
                lock_id uuid NOT NULL,
                locked_until_utc timestamptz,
                attempts integer NOT NULL DEFAULT 1,
                received_at_utc timestamptz NOT NULL DEFAULT clock_timestamp(),
                processed_at_utc timestamptz,
                last_error text,
                PRIMARY KEY (queue_name, message_id)
            );
            CREATE INDEX IF NOT EXISTS "ix_{_options.TableName}_processed"
                ON {_table} (processed_at_utc) WHERE status = 'Processed';
            """);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<InboxAcquireResult> TryAcquireAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand($"""
            INSERT INTO {_table} AS inbox
                (queue_name, message_id, status, lock_id, locked_until_utc)
            VALUES (@queue, @message, 'Processing', @owner, clock_timestamp() + @lease)
            ON CONFLICT (queue_name, message_id) DO UPDATE
            SET status = 'Processing', lock_id = @owner,
                locked_until_utc = clock_timestamp() + @lease,
                attempts = inbox.attempts + 1, last_error = NULL
            WHERE inbox.status = 'Failed'
               OR (inbox.status = 'Processing' AND inbox.locked_until_utc <= clock_timestamp())
            RETURNING lock_id;
            """, key, lockId);
        command.Parameters.AddWithValue("lease", _options.LeaseDuration);
        if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is Guid)
            return InboxAcquireResult.Acquired;

        await using var status = CreateCommand(
            $"SELECT status FROM {_table} WHERE queue_name = @queue AND message_id = @message;", key, lockId);
        return await status.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string == "Processed"
            ? InboxAcquireResult.Processed : InboxAcquireResult.Busy;
    }

    public async Task<bool> RenewAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand($"""
            UPDATE {_table} SET locked_until_utc = clock_timestamp() + @lease
            WHERE queue_name = @queue AND message_id = @message AND lock_id = @owner
              AND status = 'Processing' AND locked_until_utc > clock_timestamp();
            """, key, lockId);
        command.Parameters.AddWithValue("lease", _options.LeaseDuration);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<bool> CompleteAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand($"""
            UPDATE {_table}
            SET status = 'Processed', processed_at_utc = clock_timestamp(), locked_until_utc = NULL, last_error = NULL
            WHERE queue_name = @queue AND message_id = @message AND lock_id = @owner
              AND status = 'Processing' AND locked_until_utc > clock_timestamp();
            """, key, lockId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task FailAsync(InboxKey key, Guid lockId, string error, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand($"""
            UPDATE {_table} SET status = 'Failed', locked_until_utc = NULL, last_error = @error
            WHERE queue_name = @queue AND message_id = @message AND lock_id = @owner AND status = 'Processing';
            """, key, lockId);
        command.Parameters.AddWithValue("error", error.Length <= 4000 ? error : error[..4000]);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource.CreateCommand($"""
            DELETE FROM {_table} WHERE (queue_name, message_id) IN (
                SELECT queue_name, message_id FROM {_table}
                WHERE status = 'Processed' AND processed_at_utc < clock_timestamp() - @retention
                ORDER BY processed_at_utc LIMIT @batch FOR UPDATE SKIP LOCKED
            );
            """);
        command.Parameters.AddWithValue("retention", _options.Retention);
        command.Parameters.AddWithValue("batch", _options.CleanupBatchSize);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private NpgsqlCommand CreateCommand(string sql, InboxKey key, Guid lockId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key.QueueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key.MessageId);
        var command = _dataSource.CreateCommand(sql);
        // Match the handler registry's case-insensitive queue identity.
        command.Parameters.AddWithValue("queue", key.QueueName.ToLowerInvariant());
        command.Parameters.AddWithValue("message", key.MessageId);
        command.Parameters.AddWithValue("owner", lockId);
        return command;
    }
}
