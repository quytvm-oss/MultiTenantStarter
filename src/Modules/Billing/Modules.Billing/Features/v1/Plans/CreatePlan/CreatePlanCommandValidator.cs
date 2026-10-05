using FluentValidation;

using Modules.Billing.Contracts.v1.Plans;

namespace Modules.Billing.Features.v1.Plans.CreatePlan;

public class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(64);

        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);

        RuleFor(x => x.Currency).NotEmpty().Length(3);

        RuleFor(x => x.MonthlyBasePrice).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Interval).IsInEnum();

        When(x => x.AnnualPrice.HasValue, () =>
        {
            RuleFor(x => x.AnnualPrice).GreaterThanOrEqualTo(0);
        });
    }
}
