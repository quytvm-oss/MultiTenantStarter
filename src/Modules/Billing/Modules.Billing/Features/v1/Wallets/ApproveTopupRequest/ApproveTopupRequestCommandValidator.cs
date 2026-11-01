using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

using Modules.Billing.Contracts.v1.Wallets;

namespace Modules.Billing.Features.v1.Wallets.ApproveTopupRequest;

internal class ApproveTopupRequestCommandValidator : AbstractValidator<ApproveTopupRequestCommand>
{
    public ApproveTopupRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
