using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Rebus;

public sealed class RebusHandlerRegistry(IEnumerable<RebusHandlerDescriptor> descriptors) : IRebusHandlerRegistry
{
    private readonly RebusHandlerDescriptor[] _descriptors = descriptors.ToArray();

    public IReadOnlyList<RebusHandlerDescriptor> GetHandlers(string queueName, Type messageType)
    {
        return _descriptors.Where(x => string.Equals(x.QueueName, queueName, StringComparison.OrdinalIgnoreCase)
                        && x.MessageType.IsAssignableFrom(messageType))
            .DistinctBy(x => x.HandlerType)
            .ToArray();
    }
}
