using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Groups.CreateGroup;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Groups.CreateGroup;

public static class CreateGroupEndpoint
{
    public static RouteHandlerBuilder MapCreateGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/groups",
            async (IMediator mediator, [FromBody] CreateGroupCommand command, CancellationToken ct)
                =>
            {
                var  result = await mediator.Send(command, ct);
                return TypedResults.Created($"/api/v1/groups/{result.Id}", result);
            })
            .WithTags("Groups")
            .WithName("CreateGroup")
            .WithSummary("Creates a new group")
            .RequirePermission(IdentityPermissions.Groups.Create)
            .WithDescription("Create a new group with optional role assignments.")
            .Produces<GroupDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}