using System.Security.Claims;

using Asp.Versioning;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.Stores;
using Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Data;
using Modules.Multitenancy.Features.v1.AdjustTenantValidity;
using Modules.Multitenancy.Features.v1.CreateTenant;
using Modules.Multitenancy.Features.v1.GetTenantMigrations;
using Modules.Multitenancy.Features.v1.GetTenants;
using Modules.Multitenancy.Features.v1.GetTenantStatus;
using Modules.Multitenancy.Features.v1.ResetTenantTheme;
using Modules.Multitenancy.Provisioning;
using Modules.Multitenancy.Services;

using Shared.Identity;
using Shared.Multitenancy;

using Web.Modules;

namespace Modules.Multitenancy;

public class MultitenancyModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddScoped<ITenantService, TenantService>();
        builder.Services.AddScoped<ITenantThemeService, TenantThemeService>();
        builder.Services.AddScoped<ITenantProvisioningStarter, TenantProvisioningService>();
        builder.Services.AddScoped<ITenantProvisioningReader, TenantProvisioningService>();
        builder.Services.AddScoped<ITenantProvisioningStateWriter, TenantProvisioningService>();
        builder.Services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();
        builder.Services.AddTransient<TenantProvisioningJob>();
        
        // Singleton — the buffer survives the request scope that calls Store(...)
        // so the background Hangfire-scheduled seed scope can still TryConsume(...).
        builder.Services.AddSingleton<ITenantInitialPasswordBuffer, TenantInitialPasswordBuffer>();
        
        builder.Services.AddCustomDbContext<TenantDbContext>();
        
        builder.Services
            .AddMultiTenant<AppTenantInfo>(options =>
            {
                options.Events.OnTenantResolveCompleted = async context =>
                {
                    if (context.MultiTenantContext.StoreInfo is null) return;
                    if (context.MultiTenantContext.StoreInfo.StoreType != typeof(DistributedCacheStore<AppTenantInfo>))
                    {
                        var sp = ((HttpContext)context.Context!).RequestServices;
                        var distributedStore = sp
                            .GetRequiredService<IEnumerable<IMultiTenantStore<AppTenantInfo>>>()
                            .FirstOrDefault(s => s.GetType() == typeof(DistributedCacheStore<AppTenantInfo>));

                        await distributedStore!.AddAsync(context.MultiTenantContext.TenantInfo!);
                    }
                    await Task.CompletedTask;
                };
            })
            // ── Strategy chain — first non-null identifier wins (registration order) ──
            // ClaimStrategy no-ops here: UseMultiTenant() runs BEFORE UseAuthentication(), so User is
            // anonymous at resolution. Tenant stays header-driven; root override is post-auth middleware below.
            .WithClaimStrategy(ClaimConstants.Tenant)
            .WithHeaderStrategy(MultitenancyConstants.Identifier)
            .WithHostStrategy()
            .WithDelegateStrategy(async context =>
            {
                if (context is not HttpContext httpContext) return null;

                if (!httpContext.Request.Query.TryGetValue("tenant", out var tenantIdentifier) ||
                    string.IsNullOrEmpty(tenantIdentifier))
                    return null;

                return await Task.FromResult(tenantIdentifier.ToString());
            })
            .WithDistributedCacheStore(TimeSpan.FromMinutes(60))
            .WithStore<EFCoreStore<TenantDbContext, AppTenantInfo>>(ServiceLifetime.Scoped);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<TenantDbContext>(
                name: "db:multitenancy",
                failureStatus: HealthStatus.Unhealthy);
        // .AddCheck<TenantMigrationsHealthCheck>(
        //     name: "db:tenants-migrations",
        //     failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // ── Root-operator header override ──────────────────────────────
        // A "root"-claim caller scopes one request to another tenant via the `tenant` header (post-auth, since
        // Finbuckle's pre-auth chain has no User). Gated on claim==root + header set != root + target exists.
        app.Use(async (ctx, next) =>
        {
            var callerTenant = ctx.User?.FindFirstValue(ClaimConstants.Tenant);
            if (string.Equals(callerTenant, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            {
                var headerValue = ctx.Request.Headers[MultitenancyConstants.Identifier].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue) &&
                    !string.Equals(headerValue, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
                {
                    var store = ctx.RequestServices.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
                    var target = await store.GetAsync(headerValue).ConfigureAwait(false);
                    if (target is not null)
                    {
                        var setter = ctx.RequestServices.GetRequiredService<IMultiTenantContextSetter>();
                        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(target);
                    }
                }
            }
            await next(ctx).ConfigureAwait(false);
        });

        // ── Deactivated-tenant guard ───────────────────────────────────
        // Finbuckle resolves inactive tenants normally, so this post-auth guard rejects any request (incl.
        // anonymous login/refresh) with a non-root inactive tenant; root operators are exempt.
        app.Use(async (ctx, next) =>
        {
            var callerTenant = ctx.User?.FindFirstValue(ClaimConstants.Tenant);
            var isOperator = string.Equals(callerTenant, MultitenancyConstants.Root.Id, StringComparison.Ordinal);
            if (!isOperator)
            {
                var accessor = ctx.RequestServices.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
                var tenant = accessor.MultiTenantContext?.TenantInfo;

                // Claim strategy no-ops pre-auth, so a JWT-only (no header) request may have no resolved
                // tenant here — fall back to the caller's claim.
                if (tenant is null && !string.IsNullOrEmpty(callerTenant))
                {
                    var store = ctx.RequestServices.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
                    tenant = await store.GetAsync(callerTenant).ConfigureAwait(false);
                }

                if (tenant is not null &&
                    !string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
                {
                    if (!tenant.IsActive)
                    {
                        throw new ForbiddenException("This tenant has been deactivated. Contact your administrator.");
                    }

                    // Expiry is enforced on every request (not just at login) with a grace window:
                    // a tenant past ValidUpto still works until ValidUpto + grace, then is hard-blocked.
                    // var graceDays = ctx.RequestServices
                    //     .GetRequiredService<IOptions<TenantBillingOptions>>().Value.GraceWindowDays;
                    //var nowUtc = ctx.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
                    // var graceEndsUtc = tenant.ValidUpto.AddDays(graceDays);
                    // if (nowUtc > graceEndsUtc)
                    // {
                    //     throw new ForbiddenException("This tenant's subscription has expired. Please renew to continue.");
                    // }

                    // Inside the grace window: surface days-left so clients can warn. Set via OnStarting so
                    // the header survives even when an exception handler rewrites the response.
                    // if (nowUtc > tenant.ValidUpto)
                    // {
                    //     var daysLeft = (int)Math.Ceiling((graceEndsUtc - nowUtc).TotalDays);
                    //     var headerValue = daysLeft.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    //     ctx.Response.OnStarting(static state =>
                    //     {
                    //         var (response, value) = ((HttpResponse, string))state;
                    //         response.Headers["X-Subscription-Grace"] = value;
                    //         return Task.CompletedTask;
                    //     }, (ctx.Response, headerValue));
                    // }
                }
            }

            await next(ctx).ConfigureAwait(false);
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/tenants")
            .WithTags("Tenants")
            .WithApiVersionSet(versionSet);
        
        CreateTenantEndpoint.Map(group);
        GetTenantStatusEndpoint.Map(group);
        GetTenantsEndpoint.Map(group);
        AdjustTenantValidityEndpoint.Map(group);
        TenantMigrationsEndpoint.Map(group);
        ResetTenantThemeEndpoint.Map(group);
    }
}