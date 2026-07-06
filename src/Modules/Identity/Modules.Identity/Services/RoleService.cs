using Core.Context;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Identity;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Identity.Services;

public sealed class RoleService(RoleManager<Role> roleManager,
    IdentityDbContext context,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ICurrentUser currentUser,
    IUserPermissionService userPermissionService) : IRoleService
{
    public Task<PagedResponse<RoleDto>> GetRolesAsync(int pageNumber = 1, int pageSize = 20, string? search = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDto?> GetRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDto> CreateOrUpdateRoleAsync(string roleId, string name, string description,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDto> GetWithPermissionsAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> UpdatePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}