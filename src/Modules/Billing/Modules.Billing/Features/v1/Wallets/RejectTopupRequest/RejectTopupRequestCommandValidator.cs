using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

using Modules.Billing.Contracts.v1.Wallets;

namespace Modules.Billing.Features.v1.Wallets.RejectTopupRequest;

internal class RejectTopupRequestCommandValidator : AbstractValidator<RejectTopupRequestCommand>
{
    public RejectTopupRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
