using MessageBus.Model;

namespace MessageBus.Contracts;

public interface IMessageSender
{
    Task<ResultResponse> SendAsync(MessageContext message);
}