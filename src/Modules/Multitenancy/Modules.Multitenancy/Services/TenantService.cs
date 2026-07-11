using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Stores;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Data;

using Persistence;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Multitenancy.Services;

public class TenantService(
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IOptions<DatabaseOptions> options,
    IServiceProvider serviceProvider,
    TenantDbContext tenantDbContext,
    TimeProvider timeProvider,
    ILogger<TenantService> logger)
    : ITenantService
{
    private readonly DatabaseOptions _options = options.Value;
    private readonly TenantDbContext _tenantDbContext = tenantDbContext;

    // public async Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken)
    // {
    //     ArgumentNullException.ThrowIfNull(query);
    //
    //     IQueryable<TenantDto> tenants = _tenantDbContext.TenantInfo
    //         .AsNoTracking()
    //         .Select(x => new TenantDto()
    //         {
    //             Id = x.Id!,
    //             Name = x.Name,
    //             ConnectionString = x.ConnectionString,
    //             AdminEmail = x.AdminEmail,
    //             IsActive = x.IsActive,
    //             ValidToUp = x.ValidUpTo,
    //             Issuer = x.Issuer
    //         });
    //     
    //     tenants = ApplySorting(tenants, query.Sort);
    //     
    //     
    //     return await tenants.ToPagedResponseAsync(query, cancellationToken)
    //         .ConfigureAwait(false);
    // }

    public async Task<bool> ExistsWithIdAsync(string id, CancellationToken cancellationToken)
     => await tenantStore.GetAsync(id).ConfigureAwait(false) is not null;

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken)
    => (await tenantStore.GetAllAsync().ConfigureAwait(false)).Any(t => t.Name == name);

    public async Task<TenantStatusDto> GetStatusAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);
        var graceEnds = tenant.ValidUpTo.AddDays(10);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        string expiryState;
        if (now <= tenant.ValidUpTo)
        {
            expiryState = "Active";
        }
        else if (now <= graceEnds)
        {
            expiryState = "InGrace";
        }
        else
        {
            expiryState = "Expired";
        }
        return new TenantStatusDto
        {
            Id = tenant.Id!,
            Name = tenant.Name!,
            IsActive = tenant.IsActive,
            ValidUpto = tenant.ValidUpTo,
            HasConnectionString = !string.IsNullOrWhiteSpace(tenant.ConnectionString),
            AdminEmail = tenant.AdminEmail!,
            Issuer = tenant.Issuer,
            Plan = "tenant.Plan",
            ExpiryState = expiryState,
            GraceEndsUtc = graceEnds
        };
    }

    public async Task<string> CreateAsync(string id,
        string name,
        string? connectionString,
        string adminEmail, string? issuer, string planKey, DateTime validUpto, CancellationToken cancellationToken)
    {
        if (connectionString?.Trim() == _options.ConnectionString.Trim())
        {
            connectionString = string.Empty;
        }

        AppTenantInfo tenant = new(id, name, connectionString, adminEmail, issuer)
        {
           //Plan = planKey,
            // Set ValidUpto directly to the plan term: SetValidity() forbids moving the date backward, and
            // the ctor seeds now+1mo, so it would reject a term computed from an earlier 'now'.
            ValidUpTo = DateTime.SpecifyKind(validUpto, DateTimeKind.Utc),
        };
        
        await tenantStore.AddAsync(tenant).ConfigureAwait(false);
        await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);

        return tenant.Id;
    }
    
    public Task<string> ActivateAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> DeactivateAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<(DateTime PeriodStartUtc, DateTime ValidUpto, bool PlanChanged)> RenewAsync(string id, string newPlanKey, int termMonths, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<DateTime> AdjustValidityAsync(string id, DateTime validUpto, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);
        
        // Set directly rather than via SetValidity: this operator override is allowed to move the date
        // backward (e.g. immediate expiry / correcting a mistake), which SetValidity forbids.
        var normalized = DateTime.SpecifyKind(validUpto, DateTimeKind.Utc);
        var previous = tenant.ValidUpTo;
        tenant.ValidUpTo = normalized;
        
        await tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Multitenancy] operator adjusted tenant {TenantId} validity from {Previous:o} to {ValidUpto:o}",
                id, previous, normalized);
        }
        
        return normalized;
    }

    public async Task MigrateTenantAsync(AppTenantInfo tenantInfo, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenantInfo);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SeedTenantAsync(AppTenantInfo tenantInfo, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenantInfo);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #region method internals
    
    private async Task<AppTenantInfo> GetTenantInfoAsync(string id, CancellationToken cancellationToken = default) => 
        await tenantStore.GetAsync(id).ConfigureAwait(false) ?? 
        throw new ArgumentException($"Tenant with id {id} not found.", nameof(id));

    private async Task RefreshTenantCacheAsync(AppTenantInfo tenant)
    {
        var cacheStore = serviceProvider
            .GetServices<IMultiTenantStore<AppTenantInfo>>()
            .FirstOrDefault(x => x.GetType() == typeof(DistributedCacheStore<AppTenantInfo>));
        
        if (cacheStore is not null)
        {
            await cacheStore.UpdateAsync(tenant).ConfigureAwait(false);
        }
    }


    #endregion
}