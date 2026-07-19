using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.GetUserById;

namespace Modules.Identity.Features.v1.Users.GetUserById;

public class GetUserByIdQueryHandler(IUserService userService) : IQueryHandler<GetUserQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetAsync(query.Id, cancellationToken).ConfigureAwait(false);
    }
}