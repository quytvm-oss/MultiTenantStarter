using Mediator;

namespace Modules.Identity.Contracts.v1.Roles.UpdatePermissions;

public record UpdatePermissionsCommand : ICommand<string>
{
    public string RoleId { get; init; } = default!;

    public List<string> Permissions { get; init; } = [];
}