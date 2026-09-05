using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Webhooks.Data;
using Modules.Webhooks.Data.Configurations;
using Modules.Webhooks.Messaging;
using Modules.Webhooks.Services;

using Persistence;

using Web.HttpResilience;
using Web.MessageBus;
using Web.Modules;

namespace Modules.Webhooks;

public class WebhooksModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);


        builder.Services.AddCustomDbContext<WebhookDbContext>();
        builder.Services.AddScoped<IDbInitializer, WebhookDbInitializer>();
        builder.Services.AddSingleton<IWebhookSecretProtector, WebhookSecretProtector>();
        builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
        builder.Services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
        builder.Services.AddScoped<WebhookDispatchJob>();
        builder.Services.AddSingleton<IRebusSubscription, WebhookSubscribe>();

        builder.Services.AddHttpClient("Webhooks")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // Untrusted tenant-supplied destination: never follow redirects (a 302 could bounce
                // to an internal host) and screen the resolved IP at connect time so DNS-rebinding
                // cannot map a public hostname to an internal address after the create-time check.
                AllowAutoRedirect = false,
                // ConnectCallback = WebhookUrlGuard.ConnectAsync,
            })
            .AddResilientHttpClient(builder.Configuration);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<WebhookDbContext>(
                name: "db:webhooks",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}