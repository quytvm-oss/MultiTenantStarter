using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.Dtos;

using Modules.Billing.Contracts.v1.Plans;
using Modules.Billing.Data;

namespace Modules.Billing.Features.v1.Plans.GetPlans;

public class GetPlansQueryHandler : IQueryHandler<GetPlansQuery, IReadOnlyList<BillingPlanDto>>
{
    private readonly BillingDbContext _dbContext;

    public GetPlansQueryHandler(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyList<BillingPlanDto>> Handle(GetPlansQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var plansQuery = _dbContext.Plans.AsNoTracking();
        if (!query.IncludeInactive)
        {
            plansQuery = plansQuery.Where(p => p.IsActive);
        }

        var plans = await plansQuery.OrderBy(p => p.Key)
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);
        return plans
            .Select(p => new BillingPlanDto(p.Id, p.Key, p.Name, p.Currency, p.MonthlyBasePrice.Amount, p.OverageRates, p.IsActive, p.Interval, p.AnnualPrice?.Amount))
            .ToList();
    }
}
