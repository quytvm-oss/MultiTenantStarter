using MessageBus.Contracts;
using Microsoft.Extensions.Logging;

namespace MessageBus.Processors;

internal class InfiniteRetryProcessor : IProcessor
{
    private readonly IProcessor _inner;
    private readonly ILogger _logger;

    public InfiniteRetryProcessor(
        IProcessor inner,
        ILoggerFactory loggerFactory)
    {
        _inner = inner;
        _logger = loggerFactory.CreateLogger<InfiniteRetryProcessor>();
    }

    public async Task ProcessAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                await _inner.ProcessAsync(provider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Processor '{ProcessorName}' failed. Retrying...", _inner.ToString());
                await Task.Delay(TimeSpan.FromSeconds(2),cancellationToken).ConfigureAwait(false);
            }
    }
}