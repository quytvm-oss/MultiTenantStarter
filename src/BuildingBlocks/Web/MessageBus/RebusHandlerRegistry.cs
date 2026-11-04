namespace Web.MessageBus;

public sealed class RebusHandlerRegistry : IRebusHandlerRegistry
{
    private readonly Dictionary<(string Queue, Type Message), RebusHandlerDescriptor> _map;

    public RebusHandlerRegistry(IEnumerable<RebusHandlerDescriptor> descriptors)
    {
        var all = descriptors.ToArray();

        var duplicates = all
            .GroupBy(d => (Queue: d.QueueName.ToUpperInvariant(), d.MessageType))
            .Where(g => g.Count() > 1)
            .ToArray();

        if (duplicates.Length > 0)
        {
            var details = string.Join(Environment.NewLine, duplicates.Select(g =>
                $" - {g.Key.Queue} / {g.Key.MessageType.Name}: " +
                string.Join(", ", g.Select(d => $"{d.HandlerType.Name}"))));

            throw new InvalidOperationException(
                "Only one handler is allowed per (queue, message type). Duplicates:" +
                Environment.NewLine + details);
        }

        _map = all.ToDictionary(d => (d.QueueName.ToUpperInvariant(), d.MessageType));
    }

    public IReadOnlyList<RebusHandlerDescriptor> GetHandlers(string queueName, Type messageType)
        => _map.TryGetValue((queueName.ToUpperInvariant(), messageType), out var descriptor)
            ? [descriptor]
            : [];
}