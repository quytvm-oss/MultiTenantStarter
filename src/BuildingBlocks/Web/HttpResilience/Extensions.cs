using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Web.HttpResilience;

public static class Extensions
{
    public static IHttpClientBuilder AddResilientHttpClient(this IHttpClientBuilder builder,  IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        
        var options = configuration.GetSection(nameof(HttpResilienceOptions)).Get<HttpResilienceOptions>() ?? new HttpResilienceOptions();

        if (!options.Enabled)
        {
            return builder;
        }

        builder.AddStandardResilienceHandler(pipeline =>
        {
            pipeline.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
            pipeline.Retry.Delay = options.MedianFirstRetryDelay;

            pipeline.TotalRequestTimeout.Timeout = options.TotalTimeout;
            pipeline.AttemptTimeout.Timeout = options.AttemptTimeout;

            pipeline.CircuitBreaker.BreakDuration = options.CircuitBreakerBreakDuration;
            pipeline.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
            pipeline.CircuitBreaker.MinimumThroughput = options.CircuitBreakerMinimumThroughput;
        });
        
        return builder;
    }
}