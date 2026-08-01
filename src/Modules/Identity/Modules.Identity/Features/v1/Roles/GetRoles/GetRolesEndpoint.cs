using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Roles.GetRoles;

using Shared.Identity.Authorization;
using Shared.Persistence;

namespace Modules.Identity.Features.v1.Roles.GetRoles;

public static class GetRolesEndpoint
{
    public static RouteHandlerBuilder MapGetRolesQuery(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/roles",
            async ([AsParameters] GetRolesQuery query, IMediator mediator, CancellationToken ct) => 
                 TypedResults.Ok(await mediator.Send(query, ct)))
            .WithTags("Roles")
            .WithName("ListRoles")
            .WithSummary("List roles (paged)")
            .RequirePermission(IdentityPermissions.Roles.View)
            .Produces<PagedResponse<RoleDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .WithDescription("Retrieve roles available for the current tenant. " +
                             "Pageable via PageNumber/PageSize; filterable via Search (case-insensitive substring against name + description).");
    }
}