using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Sessions.GetUserSessions;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Sessions.GetUserSessions;

public static class GetUserSessionsEndpoint
{
    public static RouteHandlerBuilder MapGetUserSessionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.Map("/users/{userId:guid}/sessions",
            async (Guid userId, IMediator mediator, CancellationToken ct) =>
                TypedResults.Ok(await mediator.Send(new GetUserSessionsQuery(userId), ct)))
            .WithName("GetUserSessions")
            .WithSummary("Get user's sessions (Admin)")
            .RequirePermission(IdentityPermissions.Sessions.ViewAll)
            .WithDescription("Retrieve all active sessions for a specific user. Requires admin permission.")
            .Produces<IEnumerable<UserSessionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}