namespace MessageBus.Contracts;

public interface IProcessingServer : IDisposable
{
    ValueTask StartAsync(CancellationToken stoppingToken);
}