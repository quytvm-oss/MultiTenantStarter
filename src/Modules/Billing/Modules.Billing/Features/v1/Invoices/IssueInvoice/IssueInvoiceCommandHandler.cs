using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Billing.Contracts.v1.Invoices;
using Modules.Billing.Services;

namespace Modules.Billing.Features.v1.Invoices.IssueInvoice;

public sealed class IssueInvoiceCommandHandler : ICommandHandler<IssueInvoiceCommand, Guid>
{
    private readonly IBillingService _billing;

    public IssueInvoiceCommandHandler(IBillingService billing)
    {
        _billing = billing;
    }

    public async ValueTask<Guid> Handle(IssueInvoiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _billing.IssueInvoiceAsync(command.InvoiceId, command.DueAtUtc, cancellationToken).ConfigureAwait(false);
        return command.InvoiceId;
    }
}
