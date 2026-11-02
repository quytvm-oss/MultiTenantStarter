using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Rebus;

public sealed record RebusHandlerDescriptor(
    string QueueName,
    Type MessageType,
    Type HandlerType,
    string HandlerKey);

public sealed record MessageRouteDescriptor(
    Type MessageType,
    string QueueName);
