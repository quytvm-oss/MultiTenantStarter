using Mediator;

using Modules.Billing.Contracts.v1.Plans;
using Modules.Billing.Data;
using Modules.Billing.Domain;

namespace Modules.Billing.Features.v1.Plans.CreatePlan;

public class CreatePlanCommandHandler : ICommandHandler<CreatePlanCommand, Guid>
{
    private readonly BillingDbContext _dbContext;

    public CreatePlanCommandHandler(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async ValueTask<Guid> Handle(CreatePlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        var plan = BillingPlan.Create(
            command.Key,
            command.Name,
            command.Currency,
            command.MonthlyBasePrice,
            command.OverageRates,
            command.Interval,
            command.AnnualPrice
        );
        await _dbContext.Plans.AddAsync(plan, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plan.Id;
    }

}
