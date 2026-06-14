using MessageBus.Exceptions;
using Microsoft.Extensions.Options;

namespace MessageBus.Transports.Implementation.RabbitMq;

public class RabbitMqConsumerClientFactory : IConsumerClientFactory
{
    private readonly IConnectionChannelPool _connectionChannelPool;
    private readonly IOptions<RabbitMQOptions> _rabbitMqOptions;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMqConsumerClientFactory(IOptions<RabbitMQOptions> rabbitMqOptions, IConnectionChannelPool channelPool,
        IServiceProvider serviceProvider)
    {
        _rabbitMqOptions = rabbitMqOptions;
        _connectionChannelPool = channelPool;
        _serviceProvider = serviceProvider;
    }

    public async Task<IConsumerClient> CreateAsync(string groupId, byte concurrent)
    {
        try
        {
            var client = new RabbitMqConsumerClient(groupId, concurrent, _connectionChannelPool,
                _rabbitMqOptions, _serviceProvider);
            
            await client.ConnectAsync();
            
            return client;
        }
        catch (Exception e)
        {
            throw new MessageBrokerConnectException(e);
        }
    }
}