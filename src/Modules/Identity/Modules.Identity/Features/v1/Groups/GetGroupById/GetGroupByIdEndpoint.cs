using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Groups.GetGroupById;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Groups.GetGroupById;

public static class GetGroupByIdEndpoint
{
    public static RouteHandlerBuilder MapGetGroupByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/groups/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetGroupByIdQuery(id), ct)))
            .WithName("Get Group By Id")
            .WithTags("Groups")
            .WithSummary("Gets a Group By Id")
            .RequirePermission(IdentityPermissions.Groups.View)
            .WithDescription("Retrieve a specific group by its ID including roles and member count.")
            .Produces<GroupDto>(StatusCodes.Status200OK);
    }
}