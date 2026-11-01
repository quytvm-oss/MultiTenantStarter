using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Billing.Contracts.v1.Invoices;
using Modules.Billing.Services;

namespace Modules.Billing.Features.v1.Invoices.MarkInvoicePaid;

public sealed class MarkInvoicePaidCommandHandler : ICommandHandler<MarkInvoicePaidCommand, Guid>
{
    private readonly IBillingService _billing;

    public MarkInvoicePaidCommandHandler(IBillingService billing)
    {
        _billing = billing;
    }

    public async ValueTask<Guid> Handle(MarkInvoicePaidCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _billing.MarkInvoicePaidAsync(command.InvoiceId, cancellationToken).ConfigureAwait(false);
        return command.InvoiceId;
    }
}
