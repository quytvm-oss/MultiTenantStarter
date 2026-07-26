using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.GetUserGroups;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Users.GetUserGroups;

public static class GetUserGroupsEndpoint
{
    public static RouteHandlerBuilder MapGetUserGroupsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users/{userId}/groups", async (string userId, IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(new GetUserGroupsQuery(userId), cancellationToken)))
            .WithName("GetUserGroups")
            .WithSummary("Get groups for a user")
            .RequirePermission(IdentityPermissions.Groups.View)
            .WithDescription("Retrieve all groups that a specific user belongs to.")
            .Produces<IEnumerable<GroupDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}