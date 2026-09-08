using Asp.Versioning;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Auditing.Contracts;
using Modules.Auditing.Core;
using Modules.Auditing.Features.GetAuditById;
using Modules.Auditing.Features.GetAudits;
using Modules.Auditing.Features.GetAuditsByCorrelation;
using Modules.Auditing.Features.GetAuditsByTrace;
using Modules.Auditing.Features.GetAuditSummary;
using Modules.Auditing.Features.GetExceptionAudits;
using Modules.Auditing.Features.GetSecurityAudits;
using Modules.Auditing.Infrastructure.Http;
using Modules.Auditing.Infrastructure.Serialization;
using Modules.Auditing.Persistence;

using Persistence;

using Shared.Identity;

using Web.Modules;

namespace Modules.Auditing;

public class AuditingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder, bool isWebHost = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(
            Contracts.Authorization.AuditingPermissions.All);

        var services = builder.Services;

        var httpOpts = builder.Configuration.GetSection("Auditing").Get<AuditHttpOptions>() ?? new AuditHttpOptions();
        services.AddSingleton(httpOpts);

        var retentionOpts = builder.Configuration.GetSection("Auditing:Retention").Get<AuditRetentionOptions>() ?? new AuditRetentionOptions();
        services.AddSingleton(retentionOpts);
        services.AddTransient<AuditRetentionJob>();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditClient, DefaultAuditClient>();
        services.AddScoped<ISecurityAudit, SecurityAudit>();
        services.AddCustomDbContext<AuditDbContext>();
        services.AddScoped<IDbInitializer, AuditDbInitializer>();
        services.AddSingleton<IAuditSerializer, SystemTextJsonAuditSerializer>();
        services.AddHealthChecks()
            .AddDbContextCheck<AuditDbContext>(
                name: "db:auditing",
                failureStatus: HealthStatus.Unhealthy);

        // Enrichers used by Audit.Configure (scoped, run on request thread)
        services.AddScoped<IAuditMaskingService, JsonMaskingService>();
        services.AddHostedService<AuditingConfigurator>();
        services.AddScoped<IAuditScope, HttpAuditScope>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ChannelAuditPublisher>();
        services.AddSingleton<IAuditPublisher>(sp => sp.GetRequiredService<ChannelAuditPublisher>());
        services.AddScoped<ISaveChangesInterceptor, AuditingSaveChangesInterceptor>();

        services.AddSingleton<IAuditSink, SqlAuditSink>();
        services.AddSingleton<IAuditDlqSink, FileAuditDlqSink>();
        services.AddHostedService<AuditBackgroundWorker>();
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<AuditHttpMiddleware>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/audits")
            .WithTags("Audits")
            .WithApiVersionSet(apiVersionSet);

        group.MapGetAuditsEndpoint();
        group.MapGetAuditByIdEndpoint();
        group.MapGetAuditsByCorrelationEndpoint();
        group.MapGetAuditsByTraceEndpoint();
        group.MapGetAuditSummaryEndpoint();
        group.MapGetExceptionAuditsEndpoint();
        group.MapGetSecurityAuditsEndpoint();
    }
}