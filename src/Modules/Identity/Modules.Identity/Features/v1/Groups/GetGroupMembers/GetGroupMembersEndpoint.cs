using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Groups.GetGroupMembers;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Groups.GetGroupMembers;

public static class GetGroupMembersEndpoint
{
    public static RouteHandlerBuilder MapGetGroupMembersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/groups/{id:guid}/members", async (Guid id, IMediator mediator, CancellationToken ct) => 
            TypedResults.Ok(await mediator.Send(new GetGroupMembersQuery(id), ct)))
            .WithName("GetGroupMembers")
            .WithTags("Groups")
            .WithSummary("Gets a list of group members.")
            .RequirePermission(IdentityPermissions.Groups.View)
            .WithDescription("Retrieve all users that belong to a specific group.")
            .Produces<IEnumerable<GroupMemberDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}