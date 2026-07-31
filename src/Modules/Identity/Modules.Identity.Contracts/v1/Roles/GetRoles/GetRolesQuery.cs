using Mediator;

using Modules.Identity.Contracts.DTOs;

using Shared.Persistence;

namespace Modules.Identity.Contracts.v1.Roles.GetRoles;

public record GetRolesQuery : IPagedQuery, IQuery<PagedResponse<RoleDto>>
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }

    public string? Search { get; set; }
}