using Mediator;

using Modules.Identity.Contracts.DTOs;

using Shared.Persistence;

namespace Modules.Identity.Contracts.v1.Sessions.GetTenantSessions;

/// <summary>
/// Returns all sessions across the current tenant, paged and optionally
/// filtered. Used by the admin "system sessions" surface — separate from
/// the per-user GetUserSessions query because it needs a different
/// permission and a different shape (paged vs. flat list).
/// </summary>
public record GetTenantSessionsQuery() : IQuery<PagedResponse<UserSessionDto>>
{
    public bool IncludeInactive { get; set; }

    public string? Search { get; set; }

    public int PageNumber { get; init; } = 1;
    
    public int PageSize { get; init; } = 50;
};