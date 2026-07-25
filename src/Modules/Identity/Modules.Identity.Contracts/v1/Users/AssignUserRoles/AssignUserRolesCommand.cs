using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Users.AssignUserRoles;

public class AssignUserRolesCommand : ICommand<string>
{
    public required string UserId { get; init; }

    public List<UserRoleDto> UserRoles { get; set; } = new();
}