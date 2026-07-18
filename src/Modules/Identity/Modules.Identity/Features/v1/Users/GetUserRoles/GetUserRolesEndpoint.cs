using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.GetUserRoles;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Users.GetUserRoles;

public static class GetUserRolesEndpoint
{
    public static RouteHandlerBuilder MapGetUserRolesEndpoint(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/users/{id:guid}/roles",
            async (string id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                return TypedResults.Ok(await mediator.Send(new GetUserRolesQuery(id), cancellationToken));
            })
            .WithName("GetUserRoles")
            .WithSummary("Get user roles")
            .RequirePermission(IdentityPermissions.Users.View)
            .WithDescription("Retrieve the roles assigned to a specific user.")
            .Produces<IEnumerable<UserRoleDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);;
        
    }
}