using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Webhooks.Contracts.Dtos;

using Modules.Webhooks.Contracts.v1.GetWebhookDeliveries;
using Modules.Webhooks.Data;

using Shared.Persistence;

namespace Modules.Webhooks.Features.v1.GetWebhookDeliveries;

public class GetWebhookDeliveriesQueryHandler : IQueryHandler<GetWebhookDeliveriesQuery, PagedResponse<WebhookDeliveryDto>>
{
    private readonly WebhookDbContext _dbContext;

    public GetWebhookDeliveriesQueryHandler(WebhookDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async ValueTask<PagedResponse<WebhookDeliveryDto>> Handle(GetWebhookDeliveriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var dbQuery = _dbContext.WebhookDeliveries
            .AsNoTracking()
            .Where(x => x.SubscriptionId == query.SubscriptionId)
            .OrderByDescending(x => x.AttemptedAtUtc);

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new WebhookDeliveryDto()
            {
                Id = x.Id,
                SubscriptionId = x.SubscriptionId,
                AttemptCount = x.AttemptCount,
                AttemptedAtUtc = x.AttemptedAtUtc,
                HttpStatusCode = x.HttpStatusCode,
                Success = x.Success,
                ErrorMessage = x.ErrorMessage,
                EventType = x.EventType
            }).ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<WebhookDeliveryDto>
        {
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Items = items
        };
    }

}
