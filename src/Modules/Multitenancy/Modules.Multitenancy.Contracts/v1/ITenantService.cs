using Modules.Multitenancy.Contracts.Dtos;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Contracts.v1;

public interface ITenantService
{
    //Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken);
    
    Task<bool> ExistsWithIdAsync(string id, CancellationToken cancellationToken);
    
    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken);
    
    Task<TenantStatusDto> GetStatusAsync(string id, CancellationToken cancellationToken);

    Task<string> CreateAsync(string id, string name, string? connectionString, string adminEmail,string? issuer,string planKey,DateTime validUpto, CancellationToken cancellationToken);
    
    Task<string> ActivateAsync(string id, CancellationToken cancellationToken);
    
    Task<string> DeactivateAsync(string id, CancellationToken cancellationToken);

    Task<(DateTime PeriodStartUtc, DateTime ValidUpto, bool PlanChanged)> RenewAsync(string id, string newPlanKey,
        int termMonths, CancellationToken cancellationToken);

    Task<DateTime> AdjustValidityAsync(string id, DateTime validUpto, CancellationToken cancellationToken = default);

    Task MigrateTenantAsync(AppTenantInfo tenantInfo, CancellationToken cancellationToken);
    
    Task SeedTenantAsync(AppTenantInfo tenantInfo, CancellationToken cancellationToken);
}