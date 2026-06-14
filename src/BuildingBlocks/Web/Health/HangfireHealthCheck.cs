using Hangfire;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Health;

public class HangfireHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var storage = JobStorage.Current;
            using var connection = storage.GetConnection();
            return Task.FromResult(HealthCheckResult.Healthy("Hangfire storage is accessible."));
        }
        catch (Exception e)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is not accessible.", e));
        }
    }
}