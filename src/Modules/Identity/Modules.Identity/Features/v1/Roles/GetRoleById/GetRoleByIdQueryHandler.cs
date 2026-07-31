using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Roles.GetRole;

namespace Modules.Identity.Features.v1.Roles.GetRoleById;

public class GetRoleByIdQueryHandler(IRoleService roleService) : IQueryHandler<GetRoleQuery, RoleDto?>
{

    public async ValueTask<RoleDto?> Handle(GetRoleQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await roleService.GetRoleAsync(query.Id, cancellationToken)
            .ConfigureAwait(false);
    }
}