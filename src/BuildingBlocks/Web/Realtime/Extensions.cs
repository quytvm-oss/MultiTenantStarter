using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

namespace Web.Realtime;

public static class Extensions
{
    public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var redis = configuration["CachingOptions:Redis"];
        var signalr = services.AddSignalR();
        if (!string.IsNullOrWhiteSpace(redis))
        {
            signalr.AddStackExchangeRedis(redis, options => options.Configuration.ChannelPrefix =
                RedisChannel.Literal("fsh-signalr"));
        }

        services.AddSingleton<IPresenceTracker, RedisPresenceTracker>();

        return services;
    }

    public static IEndpointRouteBuilder MapHeroRealtime(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapHub<AppHub>("/api/v1/realtime/hub");

        endpoints.MapGet("/api/v1/realtime/presence",
                async ([FromQuery] string? userIds, IPresenceTracker presence, CancellationToken ct) =>
                {
                    var ids = (userIds ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    var map = await presence.GetStatusAsync(ids, ct).ConfigureAwait(false);

                    return Results.Ok(map.Select(kv => new { userId = kv.Key, online = kv.Value }));
                })
            .RequireAuthorization()
            .WithName("GetPresence")
            .WithSummary("Snapshot online status for a comma-separated list of user ids.");

        return endpoints;
    }
}