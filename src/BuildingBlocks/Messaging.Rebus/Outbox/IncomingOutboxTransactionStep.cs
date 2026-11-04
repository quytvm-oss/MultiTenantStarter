using Npgsql;
using Rebus.Config.Outbox;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Messaging.Rebus;

/// <summary>Flushes outgoing messages before committing, leaving the broker ACK to Rebus.</summary>
internal sealed class IncomingOutboxTransactionStep(NpgsqlDataSource dataSource) : IIncomingStep
{
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        await using var connection = await dataSource.OpenConnectionAsync().ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
        using var scope = new RebusTransactionScope();
        scope.UseOutbox(connection, transaction);
        scope.TransactionContext.Items[StepContext.StepContextKey] = context;
        await next().ConfigureAwait(false);
        await scope.CompleteAsync().ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
        // An exception disposes (and rolls back) the uncommitted transaction.
    }
}
