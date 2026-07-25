using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.AssignUserRoles;

namespace Modules.Identity.Features.v1.Users.AssignUserRoles;

public class AssignUserRolesCommandHandler(IUserService userService) : ICommandHandler<AssignUserRolesCommand, string>
{
    public async ValueTask<string> Handle(AssignUserRolesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await userService.AssignRolesAsync(command.UserId, command.UserRoles, cancellationToken)
            .ConfigureAwait(false);
    }
}