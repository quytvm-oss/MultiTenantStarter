using RabbitMQ.Client;

namespace MessageBus.Transports.Implementation.RabbitMq;

public interface IConnectionChannelPool
{
    string HostAddress { get; }

    string Exchange { get; }

    IConnection GetConnection();

    Task<IChannel> Rent();

    bool Return(IChannel context);
}