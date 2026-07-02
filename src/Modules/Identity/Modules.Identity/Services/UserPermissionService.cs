using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

namespace Modules.Identity.Services;

public class UserPermissionService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IdentityDbContext db,
    HybridCache cache) : IUserPermissionService
{
    public Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task InvalidatePermissionCacheAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}