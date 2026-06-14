using MessageBus.Contracts;
using Microsoft.Extensions.Logging;

namespace MessageBus.Processors;

internal class TransportConsumerCheckProcessor : IProcessor
{
    private readonly ILogger _logger;
    private readonly IConsumerRegister _register;
    private readonly TimeSpan _waitingInterval;

    public TransportConsumerCheckProcessor(ILogger<TransportConsumerCheckProcessor> logger, IConsumerRegister register)
    {
        _logger = logger;
        _register = register;
        _waitingInterval = TimeSpan.FromSeconds(30);
    }
    
    public async Task ProcessAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        _logger.LogDebug("Transport connection checking...");

        if (!_register.IsHealthy())
        {
            _logger.LogWarning("Transport connection is unhealthy, reconnection...");

            await _register.ReStartAsync();
        }
        else
        {
            _logger.LogDebug("Transport connection healthy!");
        }

        await Task.Delay(_waitingInterval, cancellationToken).ConfigureAwait(false);
    }
}