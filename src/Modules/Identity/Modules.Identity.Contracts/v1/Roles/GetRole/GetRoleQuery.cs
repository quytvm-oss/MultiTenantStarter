using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Roles.GetRole;

public record GetRoleQuery(string Id) : IQuery<RoleDto?> ;