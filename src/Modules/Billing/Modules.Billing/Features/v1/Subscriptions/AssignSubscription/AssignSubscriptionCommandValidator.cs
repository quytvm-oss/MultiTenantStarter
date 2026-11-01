using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

using Modules.Billing.Contracts.v1.Subscriptions;

namespace Modules.Billing.Features.v1.Subscriptions.AssignSubscription;

internal class AssignSubscriptionCommandValidator : AbstractValidator<AssignSubscriptionCommand>
{
    public AssignSubscriptionCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.")
            .MaximumLength(64).WithMessage("TenantId must not exceed 100 characters.");
        RuleFor(x => x.PlanKey)
            .NotEmpty().WithMessage("PlanKey is required.")
            .MaximumLength(64).WithMessage("PlanKey must not exceed 100 characters.");
    }
}
