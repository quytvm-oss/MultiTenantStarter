using System;
using System.Collections.Generic;
using System.Text;

using Modules.Billing.Contracts.Dtos;

namespace Modules.Billing.Services;

/// <summary>Renders an invoice to a self-contained PDF document (on-demand, no stored artifact).</summary>
internal interface IInvoicePdfRenderer
{
    byte[] Render(InvoiceDto invoice);
}
