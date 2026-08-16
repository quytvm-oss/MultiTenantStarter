using FluentValidation;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Notifications.Data;

using Persistence;

using Web.Modules;

namespace Modules.Notifications;

public class NotificationsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.Services.AddCustomDbContext<NotificationsDbContext>();
        builder.Services.AddScoped<IDbInitializer, NotificationsDbInitializer>();
        builder.Services.AddValidatorsFromAssembly(typeof(NotificationsModule).Assembly);
        
        builder.Services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>(
            name: "db:notifications",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}