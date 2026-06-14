namespace MessageBus.Transports;

public interface IConsumerClientFactory
{
    Task<IConsumerClient> CreateAsync(string groupName, byte groupConcurrent);
}