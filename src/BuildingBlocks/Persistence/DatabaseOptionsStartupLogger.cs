using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Persistence;

namespace Persistence;

public class DatabaseOptionsStartupLogger : IHostedService
{
    private readonly ILogger<DatabaseOptionsStartupLogger> _logger;
    private readonly IOptions<DatabaseOptions> _options;

    public DatabaseOptionsStartupLogger(ILogger<DatabaseOptionsStartupLogger> logger, IOptions<DatabaseOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("current db provider: {Provider}", options.Provider);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}