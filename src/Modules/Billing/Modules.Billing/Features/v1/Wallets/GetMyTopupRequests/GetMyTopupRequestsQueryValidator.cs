using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

using Modules.Billing.Contracts.v1.Wallets;

namespace Modules.Billing.Features.v1.Wallets.GetMyTopupRequests;

internal class GetMyTopupRequestsQueryValidator : AbstractValidator<GetMyTopupRequestsQuery>
{
    public GetMyTopupRequestsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
