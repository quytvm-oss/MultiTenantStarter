using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Auditing.Contracts;
using Modules.Auditing.Core;
using Modules.Auditing.Infrastructure.Http;
using Modules.Auditing.Infrastructure.Serialization;
using Modules.Auditing.Persistence;

using Persistence;

using Shared.Identity;

using Web.Modules;

namespace Modules.Auditing;

public class AuditingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(
            Contracts.Authorization.AuditingPermissions.All);
        
        var httpOpts = builder.Configuration.GetSection("Auditing").Get<AuditHttpOptions>() ?? new AuditHttpOptions();
        builder.Services.AddSingleton(httpOpts);

        var retentionOpts = builder.Configuration.GetSection("Auditing:Retention").Get<AuditRetentionOptions>() ?? new AuditRetentionOptions();
        builder.Services.AddSingleton(retentionOpts);
        builder.Services.AddTransient<AuditRetentionJob>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuditClient, DefaultAuditClient>();
        builder.Services.AddScoped<ISecurityAudit, SecurityAudit>();
        builder.Services.AddCustomDbContext<AuditDbContext>();
        builder.Services.AddScoped<IDbInitializer, AuditDbInitializer>();
        builder.Services.AddSingleton<IAuditSerializer, SystemTextJsonAuditSerializer>();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AuditDbContext>(
                name: "db:auditing",
                failureStatus: HealthStatus.Unhealthy);

        // Enrichers used by Audit.Configure (scoped, run on request thread)
        builder.Services.AddScoped<IAuditMaskingService, JsonMaskingService>();
        builder.Services.AddHostedService<AuditingConfigurator>();
        builder.Services.AddScoped<IAuditScope, HttpAuditScope>();

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ChannelAuditPublisher>();
        builder.Services.AddSingleton<IAuditPublisher>(sp => sp.GetRequiredService<ChannelAuditPublisher>());
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>();

        builder.Services.AddSingleton<IAuditSink, SqlAuditSink>();
        builder.Services.AddSingleton<IAuditDlqSink, FileAuditDlqSink>();
        builder.Services.AddHostedService<AuditBackgroundWorker>();
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<AuditHttpMiddleware>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        
    }
}