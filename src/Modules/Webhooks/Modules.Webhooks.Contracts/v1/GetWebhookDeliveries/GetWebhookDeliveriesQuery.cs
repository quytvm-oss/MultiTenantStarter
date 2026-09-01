using Mediator;

using Modules.Webhooks.Contracts.Dtos;

using Shared.Persistence;

namespace Modules.Webhooks.Contracts.v1.GetWebhookDeliveries;

public sealed record GetWebhookDeliveriesQuery(Guid SubscriptionId, int PageNumber = 1, int PageSize = 10)
    : IQuery<PagedResponse<WebhookDeliveryDto>>;
