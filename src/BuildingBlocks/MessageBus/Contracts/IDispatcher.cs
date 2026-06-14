using MessageBus.Model;
using MessageBus.Subscribes;

namespace MessageBus.Contracts;

public interface IDispatcher : IProcessingServer
{
    ValueTask EnqueueToPublish(MessageContext message);

    ValueTask EnqueueToExecute(MessageContext message, ConsumerExecutorRegistration? descriptor = null);

    Task EnqueueToScheduler(MessageContext message, DateTime publishTime, object? transaction = null);
}