using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Rebus;

public interface IRebusHandlerRegistry
{
    IReadOnlyList<RebusHandlerDescriptor> GetHandlers(string queueName, Type messageType);
}
