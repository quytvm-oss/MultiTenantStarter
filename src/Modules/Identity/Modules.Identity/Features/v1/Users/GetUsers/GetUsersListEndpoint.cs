using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.GetUsers;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Users.GetUsers;

public static class GetUsersListEndpoint
{
    internal static RouteHandlerBuilder MapGetListUsersEndpoint(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/users", async (CancellationToken cancellationToken, IMediator mediator) 
            => TypedResults.Ok( await mediator.Send(new GetUsersQuery(), cancellationToken)))
            .WithName("GetUsersList")
            .WithSummary("Get list of users")
            .RequirePermission(IdentityPermissions.Users.View)
            .WithDescription("Retrieve a list of users for the current tenant.")
            .Produces<IEnumerable<UserDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}