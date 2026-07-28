using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Sessions.GetMySessions;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Sessions.GetMySessions;

public static class GetMySessionsEndpoint
{
    internal static RouteHandlerBuilder MapGetMySessionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/sessions/me", async (CancellationToken ct, IMediator mediator) =>
                TypedResults.Ok(await mediator.Send(new GetMySessionsQuery(), ct)))
            .WithName("GetMySessions")
            .WithSummary("Get current user's sessions")
            .RequirePermission(IdentityPermissions.Sessions.View)
            .WithDescription("Retrieve all active sessions for the currently authenticated user.")
            .Produces<IEnumerable<UserSessionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}