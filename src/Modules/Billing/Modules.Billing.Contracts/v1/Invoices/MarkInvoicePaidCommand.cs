using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

namespace Modules.Billing.Contracts.v1.Invoices;

public sealed record MarkInvoicePaidCommand(Guid InvoiceId) : ICommand<Guid>;

