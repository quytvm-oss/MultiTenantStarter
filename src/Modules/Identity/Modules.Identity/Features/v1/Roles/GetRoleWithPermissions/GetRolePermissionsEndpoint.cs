using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Roles.GetRoleWithPermissions;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Roles.GetRoleWithPermissions;

public static class GetRolePermissionsEndpoint
{
    public static RouteHandlerBuilder MapGetRoleWithPermissionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id:guid}/permissions",async (string id, IMediator mediator, CancellationToken ct ) => 
          TypedResults.Ok(await mediator.Send(new GetRoleWithPermissionsQuery(id),ct)))
            .WithTags("Roles")
            .WithName("GetRolePermissions")
            .WithSummary("Get role permissions")
            .RequirePermission(IdentityPermissions.Roles.View)
            .WithDescription("Retrieve a role along with its assigned permissions.")
            .Produces<RoleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            ;
    }
}