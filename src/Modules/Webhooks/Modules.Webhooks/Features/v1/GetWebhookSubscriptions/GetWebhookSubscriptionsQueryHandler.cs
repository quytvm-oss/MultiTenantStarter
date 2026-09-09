using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Webhooks.Contracts.Dtos;

using Modules.Webhooks.Contracts.v1.GetWebhookSubscriptions;
using Modules.Webhooks.Data;

using Shared.Persistence;

namespace Modules.Webhooks.Features.v1.GetWebhookSubscriptions;

public class GetWebhookSubscriptionsQueryHandler : IQueryHandler<GetWebhookSubscriptionsQuery, PagedResponse<WebhookSubscriptionDto>>
{
    private readonly WebhookDbContext _dbContext;

    public GetWebhookSubscriptionsQueryHandler(WebhookDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async ValueTask<PagedResponse<WebhookSubscriptionDto>> Handle(GetWebhookSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var dbQuery = _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc);

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new WebhookSubscriptionDto()
            {
                Id = x.Id,
                Url = x.Url,
                Events = x.EventsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<WebhookSubscriptionDto>
        {
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Items = items
        };
    }

}
