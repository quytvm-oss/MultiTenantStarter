using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Shared.Identity.Authorization;

using Modules.Webhooks.Contracts.Authorization;

using Modules.Webhooks.Contracts.v1.GetWebhookDeliveries;

namespace Modules.Webhooks.Features.v1.GetWebhookDeliveries;

public static class GetWebhookDeliveriesEndpoint
{
    internal static RouteHandlerBuilder MapGetWebhookDeliveriesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/subscriptions/{subscriptionId:guid}/deliveries", async (
            Guid subscriptionId,
            int pageNumber,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWebhookDeliveriesQuery(subscriptionId, pageNumber, pageSize), ct);
            return TypedResults.Ok(result);
        })
        .WithName("GetWebhookDeliveries")
        .WithSummary("List webhook deliveries for a subscription")
        .RequirePermission(WebhooksPermissions.Subscriptions.View);
    }
}
