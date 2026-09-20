using Asp.Versioning;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Billing.Contracts.Authorization;
using Modules.Billing.Data;

using Persistence;

using Shared.Identity;

using Web.Modules;

namespace Modules.Billing;

public class BillingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder, bool IsWebHost = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(BillingPermissions.All);

        var services = builder.Services;

        services.AddCustomDbContext<BillingDbContext>();
        services.AddScoped<IDbInitializer, BillingDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<BillingDbContext>(
                name: "db:billing",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/billing")
            .WithTags("Billing")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();
    }
}