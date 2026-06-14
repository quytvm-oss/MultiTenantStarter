using MessageBus.Model;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MessageBus.Transports.Implementation.RabbitMq;

internal sealed class RabbitMqTransport : ITransport
{
    private readonly IConnectionChannelPool _pool;
    private readonly string                 _exchange;
    private readonly ILogger                _logger;

    public RabbitMqTransport(
        ILogger<RabbitMqTransport> logger,
        IConnectionChannelPool     pool)
    {
        _logger   = logger;
        _pool     = pool;
        _exchange = pool.Exchange;
    }

    public BrokerAddress BrokerAddress => new("RabbitMQ", _pool.HostAddress);

    public async Task<ResultResponse> SendAsync(TransportContext message)
    {
        IChannel? channel = null;
        try
        {
            channel = await _pool.Rent();

            var props = new BasicProperties
            {
                MessageId    = message.GetId(),
                DeliveryMode = DeliveryModes.Persistent,
                Headers      = message.Headers
                                     .ToDictionary(x => x.Key, object? (x) => x.Value)
            };

            await channel.BasicPublishAsync(
                _exchange, message.GetName(),
                mandatory: false, props, message.Body);

            _logger.LogInformation(
                "Message '{Name}' published, id='{Id}'",
                message.GetName(), message.GetId());

            return ResultResponse.Success;
        }
        catch (Exception ex)
        {
            // Channel.IsOpen = true nhưng connection thực sự đóng rồi
            // (https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/1871)
            // → Force dispose, không Return về pool để tránh channel lỗi
            if (ex is AlreadyClosedException && channel?.IsOpen == true)
            {
                _logger.LogWarning(
                    "Channel state inconsistency (IsOpen=true / connection closed). " +
                    "Force-disposing channel.");
                await channel.DisposeAsync();
                channel = null;
            }

            _logger.LogError(ex, "Failed to publish message '{Name}'", message.GetName());
            return ResultResponse.Fail(ex.Message);
        }
        finally
        {
            if (channel != null)
                _pool.Return(channel);
        }
    }
}