using Modules.Multitenancy.Contracts.Dtos;

namespace Modules.Multitenancy.Contracts.v1;

public interface ITenantThemeService
{
    Task<TenantThemeDto> GetThemeAsync(string tenantId, CancellationToken ct = default);

    Task<TenantThemeDto> GetCurrentTenantThemeAsync(CancellationToken ct = default);
    
    Task<TenantThemeDto> GetDefaultThemeAsync(CancellationToken ct = default);
    
    Task UpdateThemeAsync(string tenantId, TenantThemeDto theme, CancellationToken ct = default);
    
    Task ResetThemeAsync(string tenantId, CancellationToken ct = default);
    
    Task SetAsDefaultThemeAsync(string tenantId, CancellationToken ct = default);
    
    Task InvalidateCacheAsync(string tenantId, CancellationToken ct = default);
}