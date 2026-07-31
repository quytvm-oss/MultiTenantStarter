using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Roles.GetRole;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Roles.GetRoleById;

public static class GetRoleByIdEndpoint
{
    public static RouteHandlerBuilder MapGetRoleByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/roles/{id:guid}", async (string id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetRoleQuery(id), ct)))
            .WithName("GetRoleById")
            .WithSummary("Gets a role by id")
            .RequirePermission(IdentityPermissions.Roles.View)
            .WithDescription("Retrieve details of a specific role by its unique identifier.")
            .Produces<RoleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}