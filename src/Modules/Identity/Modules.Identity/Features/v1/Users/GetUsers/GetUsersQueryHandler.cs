using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.GetUsers;

namespace Modules.Identity.Features.v1.Users.GetUsers;

public class GetUsersQueryHandler(IUserService userService) : IQueryHandler<GetUsersQuery, List<UserDto>>
{
    
    public async ValueTask<List<UserDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        return await userService.GetListAsync(cancellationToken).ConfigureAwait(false);
    }
}