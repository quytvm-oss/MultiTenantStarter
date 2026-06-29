using Core.Context;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Identity.Data;

using Persistence;

using Web.Modules;

namespace Modules.Identity;

public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        builder.Services.AddScoped<ICurrentUser, NoCurrentUser>();
        builder.Services.AddScoped<ICurrentUserInitializer, NoCurrentUserInitializer>();
        
        services.AddCustomDbContext<IdentityDbContext>();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>(
                name: "db:identity",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
       
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
       
    }
}