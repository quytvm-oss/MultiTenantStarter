using Mediator;

using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.v1.Roles.GetRoleWithPermissions;

public record GetRoleWithPermissionsQuery(string Id) : IQuery<RoleDto>;