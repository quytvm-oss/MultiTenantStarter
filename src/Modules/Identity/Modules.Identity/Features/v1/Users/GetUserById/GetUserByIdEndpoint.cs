using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.GetUserById;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Users.GetUserById;

public static class GetUserByIdEndpoint
{
    public static RouteHandlerBuilder MapGetUserByIdEndpoint(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/users/{id:guid}", async (string id, IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(new GetUserQuery(id), cancellationToken)))
            .WithName("GetUser")
            .WithSummary("Get user by ID")
            .RequirePermission(IdentityPermissions.Users.View)
            .WithDescription("Retrieve a user's profile details by unique user identifier.")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}