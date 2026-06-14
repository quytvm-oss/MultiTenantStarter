using System.Net;
using System.Net.NetworkInformation;
using MessageBus.Contracts;
using MessageBus.Model;
using MessageBus.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageBus.Processors;

public class MessageRetryProcessor : IProcessor
{
    private readonly ILogger _logger;
    private readonly IDispatcher _dispatcher;
    private readonly TimeSpan _waitingInterval;
    private readonly IOptions<MessageBusOptions> _options;
    private readonly IDataStorage _dataStorage;
    private readonly TimeSpan _ttl;
    private readonly TimeSpan _lookbackSeconds;
    private readonly string _instance;
    private Task? _failedRetryConsumeTask;
    
    public MessageRetryProcessor(IOptions<MessageBusOptions> options, ILogger<MessageRetryProcessor> logger,
        IDispatcher dispatcher, IDataStorage dataStorage)
    {
        _options = options;
        _logger = logger;
        _dispatcher = dispatcher;
        _waitingInterval = TimeSpan.FromSeconds(options.Value.FailedRetryInterval);
        _lookbackSeconds = TimeSpan.FromSeconds(options.Value.FallbackWindowLookbackSeconds);
        _dataStorage = dataStorage;
        _ttl = _waitingInterval.Add(TimeSpan.FromSeconds(10));

        _instance = options.Value.Instance;
    }
    
    public async Task ProcessAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var storage = provider.GetRequiredService<IDataStorage>();
        
        _ = Task.Run(() => ProcessPublishedAsync(storage, cancellationToken), cancellationToken);

        if (_failedRetryConsumeTask is { IsCompleted: true })
        {
            await _dataStorage.RenewLockAsync($"received_retry", _ttl, _instance, cancellationToken);

            await Task.Delay(_waitingInterval, cancellationToken).ConfigureAwait(false);

            return;
        }

        _failedRetryConsumeTask = Task.Run(() => ProcessReceivedAsync(storage, cancellationToken), cancellationToken);
        
        _  = _failedRetryConsumeTask.ContinueWith(_ => { _failedRetryConsumeTask = null; }, cancellationToken);
        
        await Task.Delay(_waitingInterval, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessReceivedAsync(IDataStorage storage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!await storage.AcquireLockAsync($"received_retry", _ttl, _instance,cancellationToken))
            return;
        IEnumerable<MessageContext> messages = [];
        try
        {
            messages = await storage.GetReceivedMessagesOfNeedRetry(_lookbackSeconds).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Get published messages from storage failed.");
        }

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _dispatcher.EnqueueToExecute(message).ConfigureAwait(false);
        }
        
        await storage.ReleaseLockAsync($"received_retry", _instance, cancellationToken);
    }

    private async Task ProcessPublishedAsync(IDataStorage storage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!await storage.AcquireLockAsync($"publish_retry", _ttl, _instance, cancellationToken)) 
            return;
        IEnumerable<MessageContext> messages = [];
        try
        {
            messages = await storage.GetPublishedMessagesOfNeedRetry(_lookbackSeconds).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Get published messages from storage failed.");
        }

        foreach (var msg in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _dispatcher.EnqueueToPublish(msg).ConfigureAwait(false);
        }
        
        await storage.ReleaseLockAsync($"publish_retry", _instance, cancellationToken);
    }
}