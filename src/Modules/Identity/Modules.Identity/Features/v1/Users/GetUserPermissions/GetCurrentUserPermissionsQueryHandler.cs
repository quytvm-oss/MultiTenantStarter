using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.GetUserPermissions;

namespace Modules.Identity.Features.v1.Users.GetUserPermissions;

public class GetCurrentUserPermissionsQueryHandler(IUserService userService)
    : IQueryHandler<GetCurrentUserPermissionsQuery, List<string>>
{
    public async ValueTask<List<string>> Handle(GetCurrentUserPermissionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetPermissionsAsync(query.UserId,cancellationToken).ConfigureAwait(false);
    }
}