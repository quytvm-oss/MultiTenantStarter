using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

using Modules.Billing.Contracts.v1.Invoices;

namespace Modules.Billing.Features.v1.Invoices.GenerateInvoices;

internal class GenerateInvoicesCommandValidator : AbstractValidator<GenerateInvoicesCommand>
{
    public GenerateInvoicesCommandValidator()
    {
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12);
    }
}
