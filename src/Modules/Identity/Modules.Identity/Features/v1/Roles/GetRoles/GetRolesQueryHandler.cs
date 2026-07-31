using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Roles.GetRoles;

using Shared.Persistence;

namespace Modules.Identity.Features.v1.Roles.GetRoles;

public class GetRolesQueryHandler(IRoleService roleService) : IQueryHandler<GetRolesQuery, PagedResponse<RoleDto>>
{
    
    public async ValueTask<PagedResponse<RoleDto>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await roleService.GetRolesAsync(
            query.PageNumber ?? 1, 
            query.PageSize ?? 20,
            query.Search,
            cancellationToken).ConfigureAwait(false);
    }
}