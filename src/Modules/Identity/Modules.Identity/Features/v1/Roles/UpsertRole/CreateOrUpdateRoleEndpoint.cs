using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Roles.UpsertRole;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Roles.UpsertRole;

public static class CreateOrUpdateRoleEndpoint
{
    public static RouteHandlerBuilder MapCreateOrUpdateRoleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/roles", async (IMediator mediator,[FromBody] UpsertRoleCommand command, CancellationToken ct) => 
            TypedResults.Ok(await mediator.Send(command, ct)))
            .WithTags("Roles")
            .WithName("CreateOrUpdateRole")
            .WithSummary("Create or update role")
            .RequirePermission(IdentityPermissions.Roles.Create)
            .WithDescription("Create a new role or update an existing role's name and description.")
            .Produces<RoleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}