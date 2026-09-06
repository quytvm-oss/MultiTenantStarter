using Microsoft.Extensions.Configuration;

using Npgsql;

using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config.Outbox;

using Rebus.Pipeline;
using Rebus.Transport;

namespace Web.MessageBus;

public sealed class OutboxBus(
    IBus inner,
    NpgsqlDataSource dataSource) : IBus
{
    public Task Send(
        object message,
        IDictionary<string, string>? headers = null)
        => WithOutbox(() => inner.Send(message, headers));

    public Task Publish(
        object message,
        IDictionary<string, string>? headers = null)
        => WithOutbox(() => inner.Publish(message, headers));

    public Task SendLocal(
        object message,
        IDictionary<string, string>? headers = null)
        => WithOutbox(() => inner.SendLocal(message, headers));

    public Task Defer(
        TimeSpan delay,
        object message,
        IDictionary<string, string>? headers = null)
        => WithOutbox(() => inner.Defer(delay, message, headers));

    public Task DeferLocal(
        TimeSpan delay,
        object message,
        IDictionary<string, string>? headers = null)
        => WithOutbox(() => inner.DeferLocal(delay, message, headers));

    public Task Reply(
        object message,
        IDictionary<string, string>? headers = null)
        => inner.Reply(message, headers);

    public Task Subscribe<TEvent>()
        => inner.Subscribe<TEvent>();

    public Task Subscribe(Type eventType)
        => inner.Subscribe(eventType);

    public Task Unsubscribe<TEvent>()
        => inner.Unsubscribe<TEvent>();

    public Task Unsubscribe(Type eventType)
        => inner.Unsubscribe(eventType);

    public IAdvancedApi Advanced => inner.Advanced;

    private async Task WithOutbox(Func<Task> action)
    {
        // Nếu đang ở trong Rebus handler thì Rebus đã có
        // MessageContext + transaction context riêng.
        if (MessageContext.Current is not null)
        {
            await action().ConfigureAwait(false);
            return;
        }

        await using var connection =
            await dataSource.OpenConnectionAsync().ConfigureAwait(false);

        await using var transaction =
            await connection.BeginTransactionAsync().ConfigureAwait(false);

        using var scope = new RebusTransactionScope();

        scope.UseOutbox(connection, transaction);

        try
        {
            await action().ConfigureAwait(false);

            await scope.CompleteAsync().ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    public void Dispose()
    {
        inner.Dispose();
    }
}