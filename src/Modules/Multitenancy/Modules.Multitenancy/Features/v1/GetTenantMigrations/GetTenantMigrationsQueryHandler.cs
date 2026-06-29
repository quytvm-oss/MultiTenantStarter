using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1.GetTenantMigrations;
using Modules.Multitenancy.Data;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Features.v1.GetTenantMigrations;

public sealed class GetTenantMigrationsQueryHandler : 
    IQueryHandler<GetTenantMigrationsQuery,IReadOnlyCollection<TenantMigrationStatusDto>>
{
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly IServiceScopeFactory _scopeFactory;

    public GetTenantMigrationsQueryHandler(IMultiTenantStore<AppTenantInfo> tenantStore, IServiceScopeFactory scopeFactory)
    {
        _tenantStore = tenantStore;
        _scopeFactory = scopeFactory;
    }


    public async ValueTask<IReadOnlyCollection<TenantMigrationStatusDto>> Handle(GetTenantMigrationsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await _tenantStore.GetAllAsync().ConfigureAwait(false);
        
        var tenantMigrationStatuses = new List<TenantMigrationStatusDto>();

        foreach (var tenant in tenants)
        {
            var tenantStatus = new TenantMigrationStatusDto()
            {
                TenantId = tenant.Id, 
                Name = tenant.Name!, 
                IsActive = tenant.IsActive, 
                ValidUpto = tenant.ValidUpTo,
            };

            try
            {
                using IServiceScope tenantScope  = _scopeFactory.CreateScope();

                var dbContext = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();
                
                var appliedMigrations = await dbContext.Database
                    .GetAppliedMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false);
                
                var pendingMigrations = await dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false);
                
                tenantStatus.Provider = dbContext.Database.ProviderName;
                tenantStatus.LastAppliedMigration = appliedMigrations.LastOrDefault();
                tenantStatus.PendingMigrations = pendingMigrations.ToArray();
                tenantStatus.HasPendingMigrations = tenantStatus.PendingMigrations.Any();
            }
            // Per-tenant failure must not stop reporting on other tenants
            catch (Exception e)
            {
                tenantStatus.Error = e.Message;
            }
            tenantMigrationStatuses.Add(tenantStatus);
        }
        
        return tenantMigrationStatuses;
    }
}