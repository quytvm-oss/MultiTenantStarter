using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Webhooks.Data;
using Modules.Webhooks.Data.Configurations;

using Persistence;

using Web.Modules;

namespace Modules.Webhooks;

public class WebhooksModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);


        builder.Services.AddCustomDbContext<WebhookDbContext>();
        builder.Services.AddScoped<IDbInitializer, WebhookDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<WebhookDbContext>(
                name: "db:webhooks",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}