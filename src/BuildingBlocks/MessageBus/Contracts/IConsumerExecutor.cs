namespace MessageBus.Contracts;

public interface IConsumerExecutor
{
    Task Execute(object message, IServiceProvider sp);
}