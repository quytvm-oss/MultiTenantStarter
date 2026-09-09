using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Shared.Identity.Authorization;

using Modules.Webhooks.Contracts.Authorization;

using Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;

namespace Modules.Webhooks.Features.v1.DeleteWebhookSubscription;

public static class DeleteWebhookSubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapDeleteWebhookSubscriptionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/subscriptions/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteWebhookSubscriptionCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("DeleteWebhookSubscription")
        .WithSummary("Delete a webhook subscription")
        .RequirePermission(WebhooksPermissions.Subscriptions.Delete)
        .Produces(StatusCodes.Status204NoContent);
    }
}
