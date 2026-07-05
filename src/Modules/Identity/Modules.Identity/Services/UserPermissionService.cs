using Caching.V2;

using Core.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using Modules.Identity.Caching;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

using Shared.Identity;

namespace Modules.Identity.Services;

public class UserPermissionService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IdentityDbContext db,
    HybridCache cache) : IUserPermissionService
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
        Flags = HybridCacheEntryFlags.DisableCompression
    };

    private static readonly string[] Tags = { CacheKeys.Tags.Permissions };
    
    public async Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken ct = default)
    {
        var set = await GetOrLoadAsync(userId, ct).ConfigureAwait(false);
        
        // Copy to a new List<string> to preserve the public contract; ~50 ns is negligible vs the
        // JSON deserialization we'd otherwise pay per L1 hit without the [ImmutableObject] optimization.
        return [.. set.Values];
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default)
    {
        // Fast path: use the cached PermissionSet directly to avoid materializing a List<string>
        // just to check a single permission. Shares the cache entry with GetPermissionsAsync.
        var set = await GetOrLoadAsync(userId, ct).ConfigureAwait(false);
        return set.Contains(permissionName);
    }

    public Task InvalidatePermissionCacheAsync(string userId, CancellationToken ct = default)
    {
        return cache.RemoveAsync(CacheKeys.UserPermissions(userId), ct).AsTask();
    }

    #region private methods

    private ValueTask<PermissionSet> GetOrLoadAsync(string userId, CancellationToken ct)
    {
        // Stateless factory overload — the factory is a static method group, so the runtime
        // reuses a cached delegate and no closure is allocated per call (including L1 hits).
        var state = new FactoryState(userManager, roleManager, db, userId);

        return cache.GetOrCreateAsync(
            CacheKeys.UserPermissions(userId),
            state,
            LoadPermissionsAsync,
            options: EntryOptions,
            tags: Tags,
            cancellationToken: ct);
    }

    private static async ValueTask<PermissionSet> LoadPermissionsAsync(FactoryState state, CancellationToken ct)
    {
        var user = await state.UserManager.FindByIdAsync(state.UserId).ConfigureAwait(false);
        _ = user ?? throw new UnauthorizedException();
        
        var userRoles = await state.UserManager.GetRolesAsync(user).ConfigureAwait(false);
        
        var directRoleIds = await state.RoleManager.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct).ConfigureAwait(false);
        
        // Group-derived roles confer permissions too — the JWT already unions them
        // (IdentityService.AddRoleClaimsAsync) and every group mutation invalidates this
        // cache entry, so the effective set must include roles reachable via UserGroups.
        var groupRoleIds = await state.Db.GroupRoles.
            Where(gr => state.Db.UserGroups.Where(ug => ug.UserId == state.UserId)
                .Select(ug => ug.GroupId)
                .Contains(gr.GroupId))
            .Select(gr => gr.RoleId).Distinct()
            .ToListAsync(ct).ConfigureAwait(false);
          
        var roleIds = directRoleIds.Union(groupRoleIds, StringComparer.Ordinal).ToList();
        
        if (roleIds.Count == 0)
            return PermissionSet.Empty;
        
        // Single query across all role IDs — cheaper than the old N+1 loop.
        var permissions = await state.Db.RoleClaims.
            Where(rc => roleIds.Contains(rc.RoleId) && rc.ClaimType == ClaimConstants.Permission)
            .Select(rc => rc.ClaimValue!)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

        return permissions.Count == 0 ? PermissionSet.Empty : new PermissionSet([.. permissions]);
    }
    
    private readonly record struct FactoryState(UserManager<User> UserManager,
        RoleManager<Role> RoleManager,
        IdentityDbContext Db,
        string UserId);

    #endregion
}