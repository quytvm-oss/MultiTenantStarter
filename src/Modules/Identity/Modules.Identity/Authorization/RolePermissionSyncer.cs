using Caching.V2;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using Modules.Identity.Data;
using Modules.Identity.Domain;

using Shared.Identity;
using Shared.Multitenancy;

namespace Modules.Identity.Authorization;

public class RolePermissionSyncer(
    IdentityDbContext context,
    RoleManager<Role> roleManager,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<RolePermissionSyncer> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var tenantId = tenantAccessor.MultiTenantContext.TenantInfo?.Id;
        bool isRoot = tenantId == MultitenancyConstants.Root.Id;

        int basicAdded = await SyncRoleAsync(RoleConstants.Basic, PermissionConstants.Basic, ct).ConfigureAwait(false);
        
        // Admin gets all non-root permissions; the root tenant's Admin additionally gets Root permissions.
        var adminPermissions = isRoot ? PermissionConstants.Admin.Concat(PermissionConstants.Root).Distinct().ToList()
            : PermissionConstants.Admin.ToList();
        
        int adminAdded = await SyncRoleAsync(RoleConstants.Admin, adminPermissions, ct).ConfigureAwait(false);
        
        // If we wrote anything, drop the per-user permission cache so already-logged-in
        // sessions see the new perms on their next request rather than waiting for TTL.
        if (basicAdded + adminAdded > 0)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.Permissions, ct).ConfigureAwait(false);
            logger.LogInformation(
                "Permissions cache flushed due to role changes. {BasicAdded} basic permissions, {AdminAdded} admin permissions.",
                basicAdded, adminAdded
            );
        }
    }

    private async Task<int> SyncRoleAsync(string roleName, IReadOnlyList<Permission> targetPermissions, CancellationToken ct)
    {
        var role = await roleManager.Roles.SingleOrDefaultAsync(x => 
            x.Name == roleName, ct).ConfigureAwait(false);
        
        if (role is null) return 0;
        
        var existing = await context.RoleClaims.
            Where(rc => rc.RoleId == role.Id && rc.ClaimType == ClaimConstants.Permission)
            .Select(rc => rc.ClaimValue)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var toAdd = targetPermissions
            .Where(p => !existingSet.Contains(p.Name))
            .Select(rc => new RoleClaim()
            {
                RoleId = role.Id,
                ClaimType = ClaimConstants.Permission,
                ClaimValue = rc.Name,
                CreatedBy = "RolePermissionSyncer",
                CreatedOn = timeProvider.GetUtcNow()
            }).ToList();

        if (toAdd.Count == 0)   
            return 0;
        
        await context.RoleClaims.AddRangeAsync(toAdd, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Synced {Count} new permission claim(s) to '{Role}' for tenant '{Tenant}'",
                toAdd.Count,
                roleName,
                tenantAccessor.MultiTenantContext.TenantInfo?.Id);
        }
        
        return toAdd.Count;
    }
}