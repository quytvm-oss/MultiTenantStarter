using System;
using System.Collections.Generic;
using System.Text;

using Rebus.Bus;
using Rebus.Bus.Advanced;
using Npgsql;

namespace Messaging.Rebus;

public interface IModuleBus<TModule> : IBus
{
    /// <summary>Enlists outgoing messages in a caller-owned PostgreSQL transaction.</summary>
    ModuleBusTransactionScope UseTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction);
}


internal sealed class ModuleBus<TModule>(OutboxBus inner) : IModuleBus<TModule>
{
    public ModuleBusTransactionScope UseTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction)
        => inner.UseTransaction(connection, transaction);

    public Task Send(object message, IDictionary<string, string>? headers = null)
        => inner.Send(message, headers);

    public Task SendLocal(object message, IDictionary<string, string>? headers = null)
        => inner.SendLocal(message, headers);

    public Task Reply(object message, IDictionary<string, string>? headers = null)
        => inner.Reply(message, headers);

    public Task Defer(TimeSpan delay, object message, IDictionary<string, string>? headers = null)
        => inner.Defer(delay, message, headers);

    public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string>? headers = null)
        => inner.DeferLocal(delay, message, headers);

    public Task Publish(object message, IDictionary<string, string>? headers = null)
        => inner.Publish(message, headers);

    public Task Subscribe<TEvent>()
        => inner.Subscribe<TEvent>();

    public Task Subscribe(Type eventType)
        => inner.Subscribe(eventType);

    public Task Unsubscribe<TEvent>()
        => inner.Unsubscribe<TEvent>();

    public Task Unsubscribe(Type eventType)
        => inner.Unsubscribe(eventType);

    public IAdvancedApi Advanced => inner.Advanced;

    // The underlying bus is owned by Rebus, not by this wrapper.
    public void Dispose() { }
}
