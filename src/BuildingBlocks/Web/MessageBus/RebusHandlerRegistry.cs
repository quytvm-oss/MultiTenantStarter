namespace Web.MessageBus;

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