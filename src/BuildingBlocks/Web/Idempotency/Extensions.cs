using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Web.Idempotency;

public static class Extensions
{
    /// <summary>
    /// Registers idempotency infrastructure and options.
    /// Apply to specific endpoints via .WithIdempotency() extension.
    /// </summary>
    public static IServiceCollection AddHeroIdempotency(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
 
        services.AddOptions<IdempotencyOptions>()
            .BindConfiguration(nameof(IdempotencyOptions));
 
        return services;
    }
}

public static class IdempotencyEndpointExtensions
{
    /// <summary>
    /// Enables idempotency for this endpoint. Requires Idempotency-Key header on requests.
    /// Duplicate requests with the same key return the cached response.
    /// </summary>
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<IdempotencyEndpointFilter>();
    }
}