using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Web.FeatureFlags;

public static class Extensions
{
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"))
            .AddFeatureFilter<TenantFeatureFilter>();

        return services;
    }
}