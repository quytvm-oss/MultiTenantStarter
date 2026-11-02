using System;
using System.Collections.Generic;
using System.Text;

using Rebus.Bus;
using Rebus.Bus.Advanced;

namespace Messaging.Rebus;

public interface IModuleBus<TModule> : IBus { }


internal sealed class ModuleBus<TModule>(IBus inner) : IModuleBus<TModule>
{
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

    public void Dispose() { }
}
