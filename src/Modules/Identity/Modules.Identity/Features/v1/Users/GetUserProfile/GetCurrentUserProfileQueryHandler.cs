using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.GetUserProfile;

namespace Modules.Identity.Features.v1.Users.GetUserProfile;

public class GetCurrentUserProfileQueryHandler(IUserService userService)
    : IQueryHandler<GetCurrentUserProfileQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetCurrentUserProfileQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetAsync(query.UserId, cancellationToken).ConfigureAwait(false);
    }
}