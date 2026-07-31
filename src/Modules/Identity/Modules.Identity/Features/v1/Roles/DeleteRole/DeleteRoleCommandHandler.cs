using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Roles.DeleteRole;

namespace Modules.Identity.Features.v1.Roles.DeleteRole;

public class DeleteRoleCommandHandler(IRoleService roleService) : ICommandHandler<DeleteRoleCommand, Unit>
{

    public async ValueTask<Unit> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await roleService.DeleteRoleAsync(command.Id, cancellationToken)
            .ConfigureAwait(false);
        return Unit.Value;
    }
}