using FluentValidation;

using Modules.Billing.Contracts.v1.Plans;

namespace Modules.Billing.Features.v1.Plans.UpdatePlan;

public class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();

        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);

        RuleFor(x => x.MonthlyBasePrice).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Interval).IsInEnum();

        When(x => x.AnnualPrice.HasValue, () =>
        {
            RuleFor(x => x.AnnualPrice).GreaterThanOrEqualTo(0);
        });
    }
}
