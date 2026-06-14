namespace MessageBus.Contracts;

public interface IConsumerRegister : IProcessingServer
{
    bool IsHealthy();

    ValueTask ReStartAsync(bool force = false);
}