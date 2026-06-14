using System.Text;
using MessageBus.Constants;
using MessageBus.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageBus.Transports.Implementation.RabbitMq;

public class RabbitMqBasicConsumer : AsyncDefaultBasicConsumer
{
    private readonly SemaphoreSlim _semaphore;
    private readonly string _groupName;
    private readonly bool _usingTaskRun;
    private readonly Func<TransportContext, object?, Task> _msgCallback;
    private readonly Action<LogMessageEventArgs>  _logCallback;
    private readonly Func<BasicDeliverEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? _customHeadersBuilder;
    private readonly IServiceProvider _serviceProvider;
    
    
    public RabbitMqBasicConsumer(IChannel channel, 
        byte concurrent,
        string groupName,
        Func<TransportContext, object?, Task> msgCallback,
        Action<LogMessageEventArgs> logCallback,
        Func<BasicDeliverEventArgs, IServiceProvider, List<KeyValuePair<string, string>>>? customHeadersBuilder,
        IServiceProvider serviceProvider
    ) : base(channel)
    {
        _semaphore = new SemaphoreSlim(concurrent);
        _groupName = groupName;
        _usingTaskRun = concurrent > 0;
        _msgCallback = msgCallback;
        _logCallback = logCallback;
        _customHeadersBuilder = customHeadersBuilder;
        _serviceProvider = serviceProvider;
    }

    public override async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange,
        string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        if (_usingTaskRun)
        {
            await _semaphore.WaitAsync(cancellationToken);
            // Copy of the body safe to use outside the RabbitMQ thread context
            ReadOnlyMemory<byte> safeBody = body.ToArray();
            _ = Task.Run(() => Consume(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, safeBody), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Consume(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body).ConfigureAwait(false);
        }
    }

    private Task Consume(string consumerTag, ulong deliveryTag, bool redelivered, string exchange,
        string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body)
    {
        var headers = new Dictionary<string, string?>();

        if (properties.Headers != null)
            foreach (var header in properties.Headers)
            {
                if (header.Value is byte[] val)
                    headers.Add(header.Key, Encoding.UTF8.GetString(val));
                else
                    headers.Add(header.Key, header.Value?.ToString());
            }

        headers[HeaderConstant.Group] = _groupName;

        if (_customHeadersBuilder != null)
        {
            var e = new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey,
                properties, body);
            var customHeaders = _customHeadersBuilder(e, _serviceProvider);
            foreach (var customHeader in customHeaders)
            {
                headers[customHeader.Key] = customHeader.Value;
            }
        }

        var message = new TransportContext(headers, body);

        return _msgCallback(message, deliveryTag);
    }

    public async Task BasicAck(ulong deliveryTag)
    {
        if (Channel.IsOpen)
           await Channel.BasicAckAsync(deliveryTag, false);

        _semaphore.Release();
    }

    public async Task BasicReject(ulong deliveryTag)
    {
        if (Channel.IsOpen)
           await Channel.BasicRejectAsync(deliveryTag, true);

        _semaphore.Release();
    }


    protected override async Task OnCancelAsync(string[] consumerTags, CancellationToken cancellationToken = default)
    {
        await base.OnCancelAsync(consumerTags, cancellationToken);

        var args = new LogMessageEventArgs
        {
            Reason = string.Join(",", consumerTags)
        };

        _logCallback(args);
    }

    public override async Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await base.HandleBasicCancelOkAsync(consumerTag, cancellationToken);

        var args = new LogMessageEventArgs
        {
            Reason = consumerTag
        };

        _logCallback(args);
    }

    public override async Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await base.HandleBasicConsumeOkAsync(consumerTag, cancellationToken);

        var args = new LogMessageEventArgs
        {
            Reason = consumerTag
        };

        _logCallback(args);
    }

    public override async Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
    {
        await base.HandleChannelShutdownAsync(channel, reason);

        var args = new LogMessageEventArgs
        {
            Reason = reason.ReplyText
        };

        _logCallback(args);
    }
}