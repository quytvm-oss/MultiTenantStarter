using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

using Modules.Billing.Contracts.v1.Wallets;

namespace Modules.Billing.Features.v1.Wallets.CreateTopupRequest;

internal class CreateTopupRequestCommandValidator : AbstractValidator<CreateTopupRequestCommand>
{
    public CreateTopupRequestCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0m).LessThanOrEqualTo(1_000_000m);
        RuleFor(x => x.Note).MaximumLength(512);
    }
}
