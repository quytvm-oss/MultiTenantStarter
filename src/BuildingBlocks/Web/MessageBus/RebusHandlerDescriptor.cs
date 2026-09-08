namespace Web.MessageBus;

public sealed record RebusHandlerDescriptor(
    string QueueName,
    Type MessageType,
    Type HandlerType,
    string HandlerKey);