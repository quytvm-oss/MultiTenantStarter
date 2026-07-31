using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Roles.UpdatePermissions;

namespace Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

public class UpdatePermissionsCommandHandler(IRoleService roleService)
    : ICommandHandler<UpdatePermissionsCommand, string>
{
    
    public async ValueTask<string> Handle(UpdatePermissionsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await roleService.UpdatePermissionsAsync(command.RoleId, command.Permissions, cancellationToken)
            .ConfigureAwait(false);
    }
}