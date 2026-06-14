using MessageBus.Contracts;
using MessageBus.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageBus.Processors;

internal class DeleteMessageExpiredProcessor : IProcessor
{
    private const int Batch = 1000;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
    private readonly ILogger _logger;
    private readonly string[] _tableNames;
    private readonly TimeSpan _waitingInterval;
    
    public DeleteMessageExpiredProcessor(ILogger<DeleteMessageExpiredProcessor> logger, IOptions<MessageBusOptions> options,
        IStorageInitializer initializer)
    {
        _logger = logger;
        _waitingInterval = TimeSpan.FromSeconds(options.Value.CollectorDeleteInterval);

        _tableNames = [initializer.GetPublishedTableName(), initializer.GetReceivedTableName()];
    }
    
    public virtual async Task ProcessAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        foreach (var table in _tableNames)
        {
            _logger.LogDebug($"Collecting expired data from table: {table}");
            int deletedCount;
            var time = DateTime.UtcNow;
            do
            {
                deletedCount = await provider.GetRequiredService<IDataStorage>()
                    .DeleteExpiresAsync(table, time, Batch, cancellationToken).ConfigureAwait(false);

                if (deletedCount != 0)
                {
                    _logger.LogDebug($"Collected {deletedCount} expired data from table: {table}");

                    await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } 
            while (deletedCount != 0);
        }
        
        await Task.Delay(_waitingInterval, cancellationToken).ConfigureAwait(false);
    }
}