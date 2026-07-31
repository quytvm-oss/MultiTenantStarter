using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Roles.UpsertRole;

public record UpsertRoleCommand : ICommand<RoleDto>
{
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string? Description { get; set; }
};