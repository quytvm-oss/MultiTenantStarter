using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Health;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private const string key = "__health_check__";

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            };
            await _cache.SetStringAsync(key, "ok", options, cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Redis is accessible.");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("Redis is not accessible.", e);
        }
    }
}