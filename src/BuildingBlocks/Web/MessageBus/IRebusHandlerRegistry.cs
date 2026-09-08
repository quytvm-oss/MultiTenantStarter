namespace Web.MessageBus;

public interface IRebusHandlerRegistry
{
    IReadOnlyList<RebusHandlerDescriptor> GetHandlers(string queueName, Type messageType);
}