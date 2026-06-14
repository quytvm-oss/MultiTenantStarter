using System.Data.Common;
using MessageBus.Contracts;
using MessageBus.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBus.Processors;

internal class MessageDelayedProcessor : IProcessor
{
    private readonly ILogger _logger;
    private readonly IDispatcher _dispatcher;
    private readonly TimeSpan _waitingInterval;

    public MessageDelayedProcessor(ILogger<MessageDelayedProcessor> logger, IDispatcher dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _waitingInterval = TimeSpan.FromSeconds(60);
    }
    
    public async Task ProcessAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var storage = provider.GetRequiredService<IDataStorage>();
        
        await ProcessDelayedAsync(storage, cancellationToken).ConfigureAwait(false);
        
        await Task.Delay(_waitingInterval, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessDelayedAsync(IDataStorage storage,CancellationToken cancellationToken)
    {
        try
        {
            await storage.ScheduleMessagesOfDelayedAsync(
                async (transaction, messages) =>
                {
                    foreach (var message in messages)
                        await _dispatcher.EnqueueToScheduler(message, message.ExpiresAt!.Value, transaction);
                }, 
                cancellationToken);
        }
        catch (DbException ex)
        {
            _logger.LogWarning(ex, "Get delayed messages from storage failed. Retrying...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schedule delayed message failed!");
        }
    }
}