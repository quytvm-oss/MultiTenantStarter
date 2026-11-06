using Asp.Versioning;

using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Notifications.Data;
using Modules.Notifications.Features.v1.Notifications.GetUnreadCount;
using Modules.Notifications.Features.v1.Notifications.ListNotifications;
using Modules.Notifications.Features.v1.Notifications.MarkAllNotificationsRead;
using Modules.Notifications.Features.v1.Notifications.MarkNotificationRead;

using Persistence;

using Web.Modules;

namespace Modules.Notifications;

public class NotificationsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder, bool isWebHost = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        services.AddCustomDbContext<NotificationsDbContext>();
        services.AddScoped<IDbInitializer, NotificationsDbInitializer>();
        services.AddValidatorsFromAssembly(typeof(NotificationsModule).Assembly);

        services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>(
            name: "db:notifications",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/notifications")
            .WithTags("Notifications")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Literal routes first; /{id:guid}/read is the only param-route and lives last.
        group.MapListNotificationsEndpoint();              // GET /
        group.MapGetUnreadCountEndpoint();                 // GET /unread-count
        group.MapMarkAllNotificationsReadEndpoint();       // POST /read-all
        group.MapMarkNotificationReadEndpoint();           // POST /{id:guid}/read
    }
}