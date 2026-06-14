using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.Transports.Implementation.RabbitMq;

public sealed class RabbitMqMessageBusOptionsExtension : IMessageBusOptionsExtension
{
    private readonly Action<RabbitMQOptions> _configure;

    public RabbitMqMessageBusOptionsExtension(Action<RabbitMQOptions> configure)
    {
        _configure = configure;
    }

    public void AddExtendServices(IServiceCollection services)
    {

        services.Configure(_configure);
        services.AddSingleton<ITransport, RabbitMqTransport>();
        services.AddSingleton<IConsumerClientFactory, RabbitMqConsumerClientFactory>();
        services.AddSingleton<IConnectionChannelPool, ConnectionChannelPool>();
    }
}