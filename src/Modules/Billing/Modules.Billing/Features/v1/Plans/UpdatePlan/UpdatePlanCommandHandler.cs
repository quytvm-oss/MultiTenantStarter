using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.v1.Plans;
using Modules.Billing.Data;

namespace Modules.Billing.Features.v1.Plans.UpdatePlan;

public sealed class UpdatePlanCommandHandler : ICommandHandler<UpdatePlanCommand, Guid>
{

    private readonly BillingDbContext _dbContext;

    public UpdatePlanCommandHandler(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async ValueTask<Guid> Handle(UpdatePlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        var plan = await _dbContext.Plans.FirstOrDefaultAsync(p => p.Id == command.PlanId, cancellationToken).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Plan {command.PlanId} not found.");

        plan.Update(command.Name, command.MonthlyBasePrice, command.OverageRates, command.Interval, command.AnnualPrice);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plan.Id;
    }

}
