using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.GetUserRoles;

namespace Modules.Identity.Features.v1.Users.GetUserRoles;

public class GetUserRolesQueryHandler(IUserService userService) : IQueryHandler<GetUserRolesQuery, List<UserRoleDto>>
{
    public async ValueTask<List<UserRoleDto>> Handle(GetUserRolesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetUserRolesAsync(query.UserId, cancellationToken).ConfigureAwait(false);
    }
}