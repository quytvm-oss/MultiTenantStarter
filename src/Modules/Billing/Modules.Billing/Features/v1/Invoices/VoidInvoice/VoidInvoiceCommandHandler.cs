using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Billing.Contracts.v1.Invoices;
using Modules.Billing.Services;

namespace Modules.Billing.Features.v1.Invoices.VoidInvoice;

public sealed class VoidInvoiceCommandHandler : ICommandHandler<VoidInvoiceCommand, Guid>
{
    private readonly IBillingService _billing;

    public VoidInvoiceCommandHandler(IBillingService billing)
    {
        _billing = billing;
    }

    public async ValueTask<Guid> Handle(VoidInvoiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _billing.VoidInvoiceAsync(
            command.InvoiceId,
            command.Reason,
            cancellationToken).ConfigureAwait(false); 
        return command.InvoiceId;
    }
}
