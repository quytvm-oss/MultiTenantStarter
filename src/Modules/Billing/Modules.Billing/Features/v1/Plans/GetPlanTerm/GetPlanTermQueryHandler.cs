using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.v1.Plans;
using Modules.Billing.Data;

namespace Modules.Billing.Features.v1.Plans.GetPlanTerm;

public sealed class GetPlanTermQueryHandler : IQueryHandler<GetPlanTermQuery, PlanTermResponse>
{
    private readonly BillingDbContext _dbContext;

    public GetPlanTermQueryHandler(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async ValueTask<PlanTermResponse> Handle(GetPlanTermQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var key = query.PlanKey.ToLowerInvariant();

        var plan = await _dbContext.Plans.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Key == key && p.IsActive, cancellationToken).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Active plan with key '{query.PlanKey}' not found.");

        return new PlanTermResponse(
            plan.Id,
            plan.Key,
            plan.Name,
            plan.Interval,
            plan.TermMonths,
            plan.TermPrice.Amount,
            plan.Currency);
    }
}
