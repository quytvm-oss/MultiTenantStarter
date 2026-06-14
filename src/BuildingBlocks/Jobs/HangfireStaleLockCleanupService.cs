using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Npgsql;

using Shared.Persistence;

namespace Jobs;

public class HangfireStaleLockCleanupService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HangfireStaleLockCleanupService> _logger;

    public HangfireStaleLockCleanupService(IConfiguration configuration, ILogger<HangfireStaleLockCleanupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dbOptions = _configuration.GetSection(nameof(DatabaseOptions))
            .Get<DatabaseOptions>();

        if (dbOptions is null)
        {
            return;
        }
        
        var hangfireOptions = _configuration.GetSection(nameof(HangfireOptions)).Get<HangfireOptions>() ?? new HangfireOptions();
        await Task.Delay(TimeSpan.FromSeconds(hangfireOptions.IntervalDelay), stoppingToken).ConfigureAwait(false);
        
        using var timer = new PeriodicTimer(TimeSpan.FromHours(hangfireOptions.IntervalCleanup));

        do
        {
            await CleanupAsync(dbOptions.ConnectionString, TimeSpan.FromSeconds(hangfireOptions.IntervalThreshold),stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        
        
        throw new NotImplementedException();
    }

    private async Task CleanupAsync(string connectionString,TimeSpan staleThreshold, CancellationToken ct)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = new NpgsqlCommand(
                $"DELETE FROM hangfire.lock WHERE acquired < NOW() - INTERVAL '{(int)staleThreshold.TotalMinutes} minutes'",
                connection);

            int deleted = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            if (deleted > 0)
                _logger.LogWarning("Cleaned up {Count} stale Hangfire locks", deleted);
            else
                _logger.LogDebug("No stale Hangfire locks found");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not cleanup stale Hangfire locks (table may not exist yet)");
        }
    }
}