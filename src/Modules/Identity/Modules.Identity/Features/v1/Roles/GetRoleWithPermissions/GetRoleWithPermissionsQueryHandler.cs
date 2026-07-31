using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Roles.GetRoleWithPermissions;

namespace Modules.Identity.Features.v1.Roles.GetRoleWithPermissions;

public class GetRoleWithPermissionsQueryHandler(IRoleService roleService)
    : IQueryHandler<GetRoleWithPermissionsQuery, RoleDto>
{
    public async ValueTask<RoleDto> Handle(GetRoleWithPermissionsQuery query, CancellationToken cancellationToken)
    {
        return await roleService.GetWithPermissionsAsync(query.Id,cancellationToken)
            .ConfigureAwait(false);
    }
}