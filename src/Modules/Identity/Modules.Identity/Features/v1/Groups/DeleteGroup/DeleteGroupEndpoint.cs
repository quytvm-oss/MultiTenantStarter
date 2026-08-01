using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.v1.Groups.DeleteGroup;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Groups.DeleteGroup;

public static class DeleteGroupEndpoint
{
    public static RouteHandlerBuilder MapDeleteGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/groups/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteGroupCommand(id), ct);
                return TypedResults.NoContent();
            }).WithName("DeleteGroup")
            .WithTags("Groups")
            .WithSummary("Delete a group")
            .RequirePermission(IdentityPermissions.Groups.Delete)
            .WithDescription("Soft delete a group. System groups cannot be deleted.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}