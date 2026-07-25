using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.v1.Users.DeleteUser;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Users.DeleteUser;

public static class DeleteUserEndpoint
{
    internal static RouteHandlerBuilder MapDeleteUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/users/{id:guid}", async (string id, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(new DeleteUserCommand(id), cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("DeleteUser")
            .WithSummary("Delete user")
            .RequirePermission(IdentityPermissions.Users.Delete)
            .WithDescription("Delete a user by unique identifier.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

}