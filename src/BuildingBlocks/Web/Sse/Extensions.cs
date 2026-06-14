using Microsoft.Extensions.DependencyInjection;

namespace Web.Sse;

public static class Extensions
{
    
    /// <summary>
    /// Registers SSE connection manager as a singleton.
    /// </summary>
    public static IServiceCollection AddSse(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IRedisSsePublisher, RedisSsePublisher>();
        services.AddSingleton<SseConnectionManager>();
        services.AddScoped<ISseTokenService, SseTokenService>();
        return services;
    }
}