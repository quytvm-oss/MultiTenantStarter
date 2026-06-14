using MessageBus.Constants;
using MessageBus.Contracts;
using MessageBus.Exceptions;
using MessageBus.Model;
using MessageBus.Persistence;
using MessageBus.Subscribes;
using MessageBus.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageBus.Processors;

internal sealed class ConsumerRegister : IConsumerRegister
{
    private readonly ILogger<ConsumerRegister> _logger;
    private readonly MessageBusOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);

    private IConsumerClientFactory _consumerClientFactory = default!;
    private IDispatcher _dispatcher = default!;
    private IDataStorage _storage = default!;
    private ISerializer _serializer = default!;
    private SubscriptionMatcherCache _matcherCache = default!;

    private CancellationTokenSource _cts = new();
    private Task? _compositeTask;
    private int _disposed;
    private bool _isHealthy = true;
    private BrokerAddress _serverAddress;

    public ConsumerRegister(ILogger<ConsumerRegister> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = serviceProvider.GetRequiredService<IOptions<MessageBusOptions>>().Value;
    }

    public bool IsHealthy() => _isHealthy;

    public async ValueTask StartAsync(CancellationToken stoppingToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _cts.Token.Register(Dispose);

        _matcherCache = _serviceProvider.GetRequiredService<SubscriptionMatcherCache>();
        _dispatcher = _serviceProvider.GetRequiredService<IDispatcher>();
        _serializer = _serviceProvider.GetRequiredService<ISerializer>();
        _storage = _serviceProvider.GetRequiredService<IDataStorage>();
        _consumerClientFactory = _serviceProvider.GetRequiredService<IConsumerClientFactory>();

        await ExecuteAsync();

        _disposed = 0;
    }

    public async ValueTask ReStartAsync(bool force = false)
    {
        if (!IsHealthy() || force)
        {
            Pulse();
            _cts = new CancellationTokenSource();
            _isHealthy = true;
            await ExecuteAsync();
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            return;

        try
        {
            Pulse();
            _compositeTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex)
        {
            var inner = ex.InnerExceptions[0];
            if (inner is not OperationCanceledException)
                _logger.LogError(inner, "Unexpected exception during dispose.");
        }
    }

    public void Pulse()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    public async ValueTask ExecuteAsync()
    {
        var groupingMatches = _matcherCache.GetCandidatesMethodsOfGroupNameGrouped();

        foreach (var matchGroup in groupingMatches)
        {
            var limit = _matcherCache.GetGroupConcurrentLimit(matchGroup.Key);

            ICollection<string> topics;
            try
            {
                await using var client = await _consumerClientFactory.CreateAsync(matchGroup.Key, limit);
                client.OnLogCallback = WriteLog;
                topics = await client.FetchTopicsAsync(
                    matchGroup.Value.Select(x => x.Descriptor.RoutingKey));
            }
            catch (MessageBrokerConnectException e)
            {
                _isHealthy = false;
                _logger.LogError(e, e.Message);
                return;
            }

            for (var i = 0; i < _options.ConsumerThreadCount; i++)
            {
                var topicIds = topics.ToList();
                _ = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await using var client = await _consumerClientFactory.CreateAsync(matchGroup.Key, limit);

                        _serverAddress = client.BrokerAddress;

                        RegisterMessageProcessor(client);

                        await client.SubscribeAsync(topicIds);

                        await client.ListeningAsync(_pollingDelay, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                    catch (MessageBrokerConnectException e)
                    {
                        _isHealthy = false;
                        _logger.LogError(e, e.Message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, e.Message);
                    }
                }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        _compositeTask = Task.CompletedTask;
    }

    private void RegisterMessageProcessor(IConsumerClient client)
    {
        client.OnLogCallback = WriteLog;
        client.OnMessageCallback = async (transport, sender) =>
        {
            try
            {
                _logger.LogDebug("Message received. Id:{Id}, Name:{Name}.", transport.GetId(), transport.GetName());
                var name = transport.GetName();
                var group = transport.GetGroup() ?? string.Empty;

                // Tìm registration
                if (!_matcherCache.TryGetTopicExecutors(name, group, out var matches))
                {
                    _logger.LogError("Message has no matching subscriber. Name:{Name}, Group:{Group}.", name, group);
                    await client.RejectAsync(sender);
                    return;
                }

                var registration = matches[0];

                // Deserialize body thành Message
                Message origin;
                string content;
                try
                {
                    var type = registration.Descriptor.MessageType;
                    //var typeInfo = registration.Descriptor.MessageTypeInfo;
                    origin = await _serializer.DeserializeAsync(transport, type);
                    content = _serializer.Serialize(origin);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to deserialize message. Name:{Name}.", name);

                    await HandleDeserializeFailureAsync(transport, name, group, e);

                    return;
                }

                // Store và enqueue
                var messageContext = await _storage.StoreReceivedMessageAsync(name, group, origin);
                messageContext.Origin = origin;
                messageContext.Content = content;

                await _dispatcher.EnqueueToExecute(messageContext, registration);

                await client.CommitAsync(sender);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "An exception occurred when processing received message. Transport:'{Transport}'.",
                    transport);

                await client.RejectAsync(sender);
            }
        };
    }
    
    private async Task HandleDeserializeFailureAsync(TransportContext transport, string name, string group, Exception e)
    {
        var headers = new Dictionary<string, string?>(transport.Headers)
        {
            [HeaderConstant.Exception] = $"{e.GetType().Name}-->{e.Message}"
        };

        string? dataUri = transport.Body.Length != 0
            ? "data:UnknownType;base64," + Convert.ToBase64String(transport.Body.Span)
            : null;

        var exceptionMessage = new Message(headers, dataUri);
        var exceptionContent = _serializer.Serialize(exceptionMessage);

        await _storage.StoreReceivedExceptionMessageAsync(name, group, exceptionContent);

        try
        {
            _options.FailedThresholdCallback?.Invoke(new FailedInfo
            {
                ServiceProvider = _serviceProvider,
                MessageType = MessageType.Subscribe,
                Message = exceptionMessage
            });
        }
        catch (Exception callbackEx)
        {
            _logger.LogWarning(callbackEx, "FailedThresholdCallback threw an exception.");
        }
    }

    private void WriteLog(LogMessageEventArgs args)
    {
        
    }
}