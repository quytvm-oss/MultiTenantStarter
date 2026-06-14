using MessageBus.Contracts;
using MessageBus.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessageBus;

public class BusBackgroundService : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<BusBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private CancellationTokenSource? _cts;
    private bool _disposed;
    private IEnumerable<IProcessingServer> _processors = default!;

    public BusBackgroundService(IServiceProvider serviceProvider, ILogger<BusBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_cts != null)
        {
            _logger.LogWarning("Bus background service is already started.");
            return;
        }

        _logger.LogInformation("Bus background service is starting.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        _processors = _serviceProvider.GetServices<IProcessingServer>();

        try
        {
            await _serviceProvider.GetRequiredService<IStorageInitializer>()
                .InitializeAsync(_cts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            if (e is InvalidOperationException) throw;
            _logger.LogError(e, "Initializing the storage structure failed.");
        }

        _cts.Token.Register(() =>
        {
            _logger.LogInformation("Bus background service is stopping.");

            foreach (var item in _processors)
            {
                try
                {
                    item.Dispose();
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogDebug(ex, "Processor '{Processor}' was cancelled during shutdown.", 
                        item.GetType().Name);
                }
            }
        });

        foreach (var item in _processors)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                await item.StartAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while starting processor '{Processor}'.",
                    item.GetType().Name);
            }
        }

        _disposed = false;
        _logger.LogInformation("Bus background service started.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public override void Dispose()
    {
        if (_disposed) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _disposed = true;

        _logger.LogInformation("Bus background service disposed.");
    }
}