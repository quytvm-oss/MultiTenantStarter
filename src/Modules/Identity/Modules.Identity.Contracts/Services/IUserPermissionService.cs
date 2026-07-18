namespace Modules.Identity.Contracts.Services;

public interface IUserPermissionService
{
    Task<List<string>> GetPermissionsAsync(string userId, CancellationToken ct = default);
    
    Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default);
    
    Task InvalidatePermissionCacheAsync(string userId,CancellationToken ct = default);
}