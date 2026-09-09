using Asp.Versioning;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Webhooks.Data;
using Modules.Webhooks.Data.Configurations;
using Modules.Webhooks.Features.v1.CreateWebhookSubscription;
using Modules.Webhooks.Features.v1.DeleteWebhookSubscription;
using Modules.Webhooks.Features.v1.GetWebhookDeliveries;
using Modules.Webhooks.Features.v1.GetWebhookSubscriptions;
using Modules.Webhooks.Features.v1.TestWebhookSubscription;
using Modules.Webhooks.Messaging;
using Modules.Webhooks.Services;

using Persistence;

using Web.HttpResilience;
using Web.MessageBus;
using Web.Modules;

namespace Modules.Webhooks;

public class WebhooksModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder, bool isWebHost = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        services.AddCustomDbContext<WebhookDbContext>();
        services.AddScoped<IDbInitializer, WebhookDbInitializer>();
        services.AddSingleton<IWebhookSecretProtector, WebhookSecretProtector>();
        services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
        services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
        services.AddScoped<WebhookDispatchJob>();
        services.AddSingleton<IRebusSubscription, WebhookSubscribe>();

        services.AddHttpClient("Webhooks")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // Untrusted tenant-supplied destination: never follow redirects (a 302 could bounce
                // to an internal host) and screen the resolved IP at connect time so DNS-rebinding
                // cannot map a public hostname to an internal address after the create-time check.
                AllowAutoRedirect = false,
                ConnectCallback = WebhookUrlGuard.ConnectAsync,
            })
            .AddResilientHttpClient(builder.Configuration);

        services.AddHealthChecks()
            .AddDbContextCheck<WebhookDbContext>(
                name: "db:webhooks",
                failureStatus: HealthStatus.Unhealthy);

        if (isWebHost)
            services.AddWebhooksMessaging(builder.Configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/webhooks")
            .WithTags("Webhooks")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapCreateWebhookSubscriptionEndpoint();
        group.MapDeleteWebhookSubscriptionEndpoint();
        group.MapGetWebhookSubscriptionsEndpoint();
        group.MapGetWebhookDeliveriesEndpoint();
        group.MapTestWebhookSubscriptionEndpoint();
    }
}