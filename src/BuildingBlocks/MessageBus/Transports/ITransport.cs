using MessageBus.Model;

namespace MessageBus.Transports;

public interface ITransport
{
    BrokerAddress BrokerAddress { get; }

    Task<ResultResponse> SendAsync(TransportContext message);
}