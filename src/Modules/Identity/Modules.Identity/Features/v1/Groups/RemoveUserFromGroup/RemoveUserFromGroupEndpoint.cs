using Amazon.S3.Util;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public static class RemoveUserFromGroupEndpoint
{
    public static RouteHandlerBuilder MapRemoveUserFromGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/groups/{groupId:guid}/members/{userId}", async (Guid groupId, string userId,
                IMediator mediator, CancellationToken ct)
            =>
        {
            await mediator.Send(new RemoveUserFromGroupCommand(groupId, userId), ct);
            return TypedResults.NoContent();
        })
        .WithName("RemoveUserFromGroup")
        .WithTags("Groups")
        .WithSummary("Removes a user from a group with given ID and user ID.")
        .RequirePermission(IdentityPermissions.Groups.ManageMembers)
        .WithDescription("Removes a user from a group with given ID and user ID.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}