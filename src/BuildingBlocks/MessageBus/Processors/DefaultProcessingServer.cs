using MessageBus.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBus.Processors;

internal class DefaultProcessingServer : IProcessingServer
{
    private CancellationTokenSource _cts;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _provider;

    private Task? _compositeTask;
    private bool _disposed;

    public DefaultProcessingServer(ILogger<DefaultProcessingServer> logger, ILoggerFactory loggerFactory, IServiceProvider provider)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _provider = provider;
        _cts = new CancellationTokenSource();
    }
    
    public ValueTask StartAsync(CancellationToken stoppingToken)
    {
        if (_disposed || _cts.IsCancellationRequested)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _disposed = false;
        }

        stoppingToken.Register(() => _cts.Cancel());

        var processorTasks = GetProcessors()
            .Select(InfiniteRetry)
            .Select(p => p.ProcessAsync(_provider, _cts.Token));
        _compositeTask = Task.WhenAll(processorTasks);

        return ValueTask.CompletedTask;
    }
    
    private IProcessor InfiniteRetry(IProcessor inner)
    {
        return new InfiniteRetryProcessor(inner, _loggerFactory);
    }

    private IProcessor[] GetProcessors()
    {
        var returnedProcessors = new List<IProcessor>
        {
            _provider.GetRequiredService<TransportConsumerCheckProcessor>(),
            _provider.GetRequiredService<MessageRetryProcessor>(),
            _provider.GetRequiredService<MessageDelayedProcessor>(),
            _provider.GetRequiredService<DeleteMessageExpiredProcessor>()
        };

        return returnedProcessors.ToArray();
    }
    
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _disposed = true;

            _logger.LogInformation("Processing server is shutting down.");
            _cts.Cancel();

            _compositeTask?.Wait((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
        }
        catch (AggregateException ex)
        {
            var inner = ex.InnerExceptions[0];
            if (inner is not OperationCanceledException)
                _logger.LogError(inner, "Unexpected exception during shutdown.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An exception occurred when disposing processing server.");
        }
        finally
        {
            _logger.LogInformation("Processing server stopped.");
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}