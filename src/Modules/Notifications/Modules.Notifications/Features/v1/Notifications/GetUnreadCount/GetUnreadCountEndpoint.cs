using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Notifications.Contracts.Authorization;
using Modules.Notifications.Contracts.v1.Queries;

using Shared.Identity.Authorization;

namespace Modules.Notifications.Features.v1.Notifications.GetUnreadCount;

public static class GetUnreadCountEndpoint
{
    internal static RouteHandlerBuilder MapGetUnreadCountEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/unread-count",
                async (IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetUnreadCountQuery(), cancellationToken)))
            .WithName("GetUnreadNotificationCount")
            .WithSummary("Count of caller's unread notifications (bell badge)")
            .RequirePermission(NotificationPermissions.Inbox.View);
}
