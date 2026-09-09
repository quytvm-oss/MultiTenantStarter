using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Shared.Identity.Authorization;

using Modules.Webhooks.Contracts.Authorization;

using Modules.Webhooks.Contracts.v1.TestWebhookSubscription;

namespace Modules.Webhooks.Features.v1.TestWebhookSubscription;

public static class TestWebhookSubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapTestWebhookSubscriptionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/subscriptions/{id:guid}/test", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var success = await mediator.Send(new TestWebhookSubscriptionCommand(id), ct);
            return TypedResults.Ok(new { Success = success });
        })
        .WithName("TestWebhookSubscription")
        .WithSummary("Send a test event to a webhook subscription")
        .RequirePermission(WebhooksPermissions.Subscriptions.Test);
    }
}
