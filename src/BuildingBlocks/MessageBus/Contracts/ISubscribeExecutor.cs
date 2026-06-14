using MessageBus.Model;
using MessageBus.Subscribes;

namespace MessageBus.Contracts;

public interface ISubscribeExecutor
{
    Task<ResultResponse> ExecuteAsync(MessageContext message, ConsumerExecutorRegistration? descriptor = null, CancellationToken cancellationToken = default);
}