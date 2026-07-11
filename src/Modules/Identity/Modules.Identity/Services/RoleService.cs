using System.Net;
using System.Security.Claims;

using Core.Context;
using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

using Shared.Identity;
using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Identity.Services;

public sealed class RoleService(
    RoleManager<Role> roleManager,
    IdentityDbContext context,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ICurrentUser currentUser,
    IUserPermissionService userPermissionService) : IRoleService
{
    public async Task<PagedResponse<RoleDto>> GetRolesAsync(int pageNumber = 1, int pageSize = 20,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 200);

        var query = roleManager.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower().Trim();
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(term)
                                     || (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var roles = await query.OrderBy(r => r.Name)
            .Skip((page - 1) * size).Take(size)
            .Select(r => new RoleDto() { Id = r.Id, Name = r.Name!, Description = r.Description, })
            .ToListAsync(cancellationToken);

        return new PagedResponse<RoleDto>
        {
            Items = roles,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = size,
            TotalPages = (int)Math.Ceiling(totalCount / (double)size)
        };
    }

    public async Task<RoleDto?> GetRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        _ = role ?? throw new NotFoundException("Role not found.");

        return new RoleDto() { Id = role.Id, Name = role.Name!, Description = role.Description, };
    }

    public async Task<RoleDto> CreateOrUpdateRoleAsync(string roleId, string name, string description,
        CancellationToken cancellationToken = default)
    {
        Role? role = string.IsNullOrEmpty(roleId)
            ? null
            : await roleManager.FindByIdAsync(roleId);

        if (role is not null)
        {
            EnsureNotSystemRole(name, "System roles cannot be created or updated.");

            EnsureNotSystemRole(name, "Cannot rename a role to a system role's name.");

            role.Name = name;
            role.Description = description;
            await roleManager.UpdateAsync(role);
        }
        else
        {
            EnsureNotSystemRole(name, "Cannot create a role using a system role's name.");

            role = new Role(name, description);
            await roleManager.CreateAsync(role);
        }

        return new RoleDto() { Id = role.Id, Name = role.Name!, Description = role.Description };
    }

    public async Task DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        Role? role = await roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException("role not found");

        EnsureNotSystemRole(role.Name, "System roles cannot be deleted.");

        await InvalidateAffectedUsersAsync(id, cancellationToken).ConfigureAwait(false);

        await roleManager.DeleteAsync(role);
    }

    public async Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await GetRoleAsync(id, cancellationToken);
        _ = role ?? throw new NotFoundException("role not found");

        role.Permissions = await context.RoleClaims
            .AsNoTracking()
            .Where(rc => rc.RoleId == id && rc.ClaimType == ClaimConstants.Permission)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync(cancellationToken);

        return role;
    }

    public async Task<string> UpdatePermissionsAsync(string roleId, List<string> permissions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var role = await roleManager.FindByIdAsync(roleId)
                   ?? throw new NotFoundException("role not found");
        
        EnsureNotSystemRole(role.Name, "System role permissions are managed by the framework and cannot be modified.");
        FilterRootPermissions(permissions);
        
        var currentClaims = await roleManager.GetClaimsAsync(role);
        await RemoveRevokedPermissionsAsync(role, currentClaims, permissions, cancellationToken);
        await AddNewPermissionsAsync(role, currentClaims, permissions, cancellationToken);
        
        await InvalidateAffectedUsersAsync(roleId, cancellationToken).ConfigureAwait(false);
        
        return "Permissions updated successfully.";
    }

    #region internals

    private static void EnsureNotSystemRole(string? roleName, string message)
    {
        if (!string.IsNullOrEmpty(roleName) && RoleConstants.IsDefault(roleName))
        {
            throw new CustomException(message, Array.Empty<string>(), HttpStatusCode.BadRequest);
        }
    }

    // Invalidate every user whose effective permissions may have shifted from a role mutation:
    // direct holders (AspNetUserRoles) and group-derived holders (members of groups carrying this role).
    private async Task InvalidateAffectedUsersAsync(string roleId, CancellationToken cancellationToken)
    {
        // var role = await roleManager.FindByIdAsync(roleId);
        // if (role?.Name is null)
        // {
        //     return;
        // }

        var directUserIds = await context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

        var groupUserIds = await context.GroupRoles
            .Where(gr => gr.RoleId == roleId)
            .SelectMany(gr => context.UserGroups.Where(ug => ug.GroupId == gr.GroupId)
                .Select(ug => ug.UserId))
            .ToListAsync(cancellationToken);

        foreach (var userId in directUserIds.Concat(groupUserIds).Distinct())
        {
            await userPermissionService.InvalidatePermissionCacheAsync(userId, cancellationToken).ConfigureAwait(false);
        }
    }

    private void FilterRootPermissions(List<string> permissions)
    {
        if (multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id == MultitenancyConstants.Root.Id)
        {
            return;
        }

        var rootOnly = PermissionConstants.Root.Select(r => r.Name).ToHashSet(StringComparer.Ordinal);
        permissions.RemoveAll(rootOnly.Contains);
    }

    private async Task AddNewPermissionsAsync(Role role, IList<Claim> currentClaims,
        List<string> permissions, CancellationToken cancellationToken = default)
    {
        var newPermissions = permissions.Where(p => !string.IsNullOrEmpty(p) && currentClaims.All(c => c.Value != p))
            .ToList();

        foreach (var permission in newPermissions)
        {
            context.RoleClaims.Add(new RoleClaim()
            {
                RoleId = role.Id,
                ClaimType = ClaimConstants.Permission,
                ClaimValue = permission,
                CreatedBy = currentUser.GetUserId().ToString(),
            });
        }

        if (newPermissions.Any())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RemoveRevokedPermissionsAsync(Role role, IList<Claim> currentClaims,
        List<string> permissions, CancellationToken cancellationToken = default)
    {
        var claimsToRemove = currentClaims.Where(c => !permissions.Exists(p => p == c.Value));

        foreach (var claim in claimsToRemove)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(error => error.Description).ToList();
                throw new CustomException("operation failed", errors);
            }
        }
    }

   #endregion
}