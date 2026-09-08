namespace Web.MessageBus;

public sealed record RebusHandlerDescriptor(
    string QueueName,
    Type MessageType,
    Type HandlerType,
    string HandlerKey);

public sealed record MessageRouteDescriptor(
    Type MessageType,
    string QueueName);