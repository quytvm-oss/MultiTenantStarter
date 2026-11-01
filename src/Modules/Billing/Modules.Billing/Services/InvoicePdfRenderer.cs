using System;
using System.Collections.Generic;
using System.Text;

using Modules.Billing.Contracts.Dtos;

namespace Modules.Billing.Services;

/// <summary>
/// QuestPDF-based invoice renderer. QuestPDF's Community license is free for organisations under
/// $1M USD/year revenue; larger downstream users must obtain a license. The dependency is isolated
/// behind <see cref="IInvoicePdfRenderer"/> so it can be swapped without touching callers.
/// </summary>
public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{


    public byte[] Render(InvoiceDto invoice)
    {
        throw new NotImplementedException();
    }
}
