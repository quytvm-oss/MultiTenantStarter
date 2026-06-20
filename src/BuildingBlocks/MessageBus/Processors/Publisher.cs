using System.Globalization;
using MessageBus.Constants;
using MessageBus.Contracts;
using MessageBus.Model;
using MessageBus.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageBus.Processors;

internal sealed class Publisher : IBusPublisher
{
    private readonly IDispatcher _dispatcher;
    private readonly IDataStorage _storage;
    private readonly IOptions<MessageBusOptions> _options;
    private readonly ILogger<Publisher> _logger;
    private readonly AsyncLocal<BusTransactionHolder> _asyncLocal;

    public Publisher(
        IServiceProvider serviceProvider,
        IDispatcher dispatcher,
        IDataStorage storage,
        IOptions<MessageBusOptions> options,
        ILogger<Publisher> logger
        )
    {
        ServiceProvider = serviceProvider;
        _dispatcher = dispatcher;
        _storage = storage;
        _options = options;
        _logger = logger;
        _asyncLocal = new AsyncLocal<BusTransactionHolder>();
    }

    public IServiceProvider ServiceProvider { get; }
    
    public IBusTransaction? Transaction
    {
        get => _asyncLocal.Value?.Transaction;
        set
        {
            _asyncLocal.Value ??= new BusTransactionHolder();
            _asyncLocal.Value.Transaction = value;
        }
    }

    public async Task PublishAsync<T>(T message, Action<PublishOptions>? configure = null, CancellationToken ct = default)
    {
        var publishOptions = new PublishOptions();
        configure?.Invoke(publishOptions);

        var routingKey = NormalizeRoutingKey(publishOptions.Name ?? typeof(T).Name);
        var headers = BuildHeaders(routingKey, publishOptions);
        var msg = new Message(headers, message);

        try
        {
            if (Transaction?.DbTransaction == null)
            {
                var messageContext = await _storage.StoreMessageAsync(routingKey, msg).ConfigureAwait(false);

                if (publishOptions.Delay.HasValue)
                    await _dispatcher.EnqueueToScheduler(messageContext, DateTime.UtcNow.Add(publishOptions.Delay.Value)).ConfigureAwait(false);
                else
                    await _dispatcher.EnqueueToPublish(messageContext).ConfigureAwait(false);
            }
            else
            {
                var messageContext = await _storage.StoreMessageAsync(routingKey, msg, Transaction.DbTransaction).ConfigureAwait(false);

                Transaction.AddToBuffer(messageContext);

                if (Transaction.AutoCommit)
                    await Transaction.CommitAsync(ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message '{RoutingKey}'.", routingKey);
            throw;
        }
    }

    public void Publish<T>(T message, Action<PublishOptions>? configure)
    {
        PublishAsync(message, null, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private IDictionary<string, string?> BuildHeaders(string routingKey, PublishOptions options)
    {
        var headers = new Dictionary<string, string?>(options.Header.ToDictionary(k => k.Key, v => (string?)v.Value))
        {
            [HeaderConstant.MessageId] = Snowflake.NewId().ToString(),
            [HeaderConstant.MessageName] = routingKey,
            [HeaderConstant.SentTime] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
        };

        if (options.Delay.HasValue)
            headers[HeaderConstant.DelayTime] = options.Delay.Value.ToString();
        
        if (!string.IsNullOrWhiteSpace(options.TenantId))
            headers[HeaderConstant.TenantId] = options.TenantId;

        if (!string.IsNullOrWhiteSpace(options.Source))
            headers[HeaderConstant.Source] = options.Source;

        return headers;
    }

    private string NormalizeRoutingKey(string routingKey)
    {
        var prefix = _options.Value.TopicNamePrefix;
        return string.IsNullOrEmpty(prefix) ? routingKey : $"{prefix}.{routingKey}";
    }
}