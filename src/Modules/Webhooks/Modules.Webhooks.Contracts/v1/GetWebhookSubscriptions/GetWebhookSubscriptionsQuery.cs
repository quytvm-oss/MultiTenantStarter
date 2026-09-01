using Mediator;

using Modules.Webhooks.Contracts.Dtos;

using Shared.Persistence;

namespace Modules.Webhooks.Contracts.v1.GetWebhookSubscriptions;

public sealed record GetWebhookSubscriptionsQuery(int PageNumber = 1, int PageSize = 10)
    : IQuery<PagedResponse<WebhookSubscriptionDto>>;
