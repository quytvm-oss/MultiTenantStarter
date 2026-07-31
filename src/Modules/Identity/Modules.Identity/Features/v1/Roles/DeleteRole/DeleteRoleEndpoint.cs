using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.v1.Roles.DeleteRole;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Roles.DeleteRole;

public static class DeleteRoleEndpoint
{
    public static RouteHandlerBuilder MapDeleteRoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/roles/{id:guid}", async (string id, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteRoleCommand(id), ct);
                return TypedResults.NoContent();
            })
            .WithName("DeleteRole")
            .WithSummary("Deletes a role")
            .RequirePermission(IdentityPermissions.Roles.Delete)
            .WithDescription("Remove an existing role by its unique identifier.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}