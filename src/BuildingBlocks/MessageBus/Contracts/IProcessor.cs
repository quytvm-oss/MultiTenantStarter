namespace MessageBus.Contracts;

public interface IProcessor
{
    Task ProcessAsync(IServiceProvider provider, CancellationToken cancellationToken);
}