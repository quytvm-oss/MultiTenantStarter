using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.DeleteUser;

namespace Modules.Identity.Features.v1.Users.DeleteUser;

public class DeleteUserCommandHandler(IUserService userService) : ICommandHandler<DeleteUserCommand, Unit>
{

    public async ValueTask<Unit> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await userService.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}