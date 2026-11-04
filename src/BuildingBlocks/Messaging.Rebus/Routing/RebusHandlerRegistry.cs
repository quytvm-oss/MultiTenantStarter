using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Rebus;

public sealed class RebusHandlerRegistry : IRebusHandlerRegistry
{
    private readonly Dictionary<(string Queue, Type Message), RebusHandlerDescriptor> _handlers;

    public RebusHandlerRegistry(IEnumerable<RebusHandlerDescriptor> descriptors)
    {
        _handlers = new();
        foreach (var descriptor in descriptors)
        {
            var key = (descriptor.QueueName.ToUpperInvariant(), descriptor.MessageType);
            if (!_handlers.TryAdd(key, descriptor))
                throw new InvalidOperationException(
                    $"Duplicate handlers for queue '{descriptor.QueueName}', message '{descriptor.MessageType.FullName}': " +
                    $"'{_handlers[key].HandlerType.FullName}' and '{descriptor.HandlerType.FullName}'.");
        }
    }

    public IReadOnlyList<RebusHandlerDescriptor> GetHandlers(string queueName, Type messageType)
    {
        return _handlers.TryGetValue((queueName.ToUpperInvariant(), messageType), out var descriptor)
            ? [descriptor] : [];
    }
}
