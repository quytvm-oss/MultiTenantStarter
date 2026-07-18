using System.Security.Claims;

using Core.Exceptions;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.v1.Users.GetUserPermissions;

using Shared.Identity.Claims;

namespace Modules.Identity.Features.v1.Users.GetUserPermissions;

public static class GetUserPermissionsEndpoint
{
    public static RouteHandlerBuilder MapGetCurrentUserPermissionsEndpoint(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/permissions", async (ClaimsPrincipal user, IMediator mediator,  CancellationToken cancellationToken) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }

            return TypedResults.Ok(await mediator.Send(new GetCurrentUserPermissionsQuery(userId),cancellationToken));
        })
        .WithName("GetCurrentUserPermissions")
        .WithSummary("Get current user permissions")
        .WithDescription("Retrieve permissions for the authenticated user. Requires authentication only — every signed-in user can read their own grants.")
        .Produces<IEnumerable<string>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}