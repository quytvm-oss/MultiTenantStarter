using System.Security.Claims;

using Core.Exceptions;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.GetUserProfile;

using Shared.Identity.Claims;

namespace Modules.Identity.Features.v1.Users.GetUserProfile;

public static class GetUserProfileEndpoint
{
    internal static RouteHandlerBuilder MapGetMeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/profile", async (ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken) =>
            {
                if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException();
                }

                return TypedResults.Ok(await mediator.Send(new GetCurrentUserProfileQuery(userId), cancellationToken));
            })
            .WithName("GetCurrentUserProfile")
            .WithSummary("Get current user profile")
            .WithDescription("Retrieve the authenticated user's profile from the access token.")
            .RequireAuthorization()
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}