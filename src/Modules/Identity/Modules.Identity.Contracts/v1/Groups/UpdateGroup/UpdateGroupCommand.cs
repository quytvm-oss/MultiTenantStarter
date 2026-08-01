using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Groups.UpdateGroup;

public record UpdateGroupCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    IReadOnlyList<string>? RoleIds) : ICommand<GroupDto>;