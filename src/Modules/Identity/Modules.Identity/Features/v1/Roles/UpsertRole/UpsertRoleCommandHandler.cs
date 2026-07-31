using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Roles.UpsertRole;

namespace Modules.Identity.Features.v1.Roles.UpsertRole;

public class UpsertRoleCommandHandler(IRoleService roleService) : ICommandHandler<UpsertRoleCommand, RoleDto>
{
    public async ValueTask<RoleDto> Handle(UpsertRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await roleService.CreateOrUpdateRoleAsync(command.Id, command.Name, command.Description ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);
    }
}