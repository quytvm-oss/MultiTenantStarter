using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Groups.GetGroups;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Groups.GetGroups;

public static class GetGroupsEndpoint
{
    public static RouteHandlerBuilder MapGetGroupsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/groups", async (IMediator mediator, string? search, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(new GetGroupsQuery(search), ct)))
            .WithName("ListGroups")
            .WithTags("Groups")
            .WithSummary("List all groups")
            .RequirePermission(IdentityPermissions.Groups.View)
            .WithDescription("Retrieve all groups for the current tenant with optional search filter.")
            .Produces<IEnumerable<GroupDto>>(StatusCodes.Status200OK);
    }
}