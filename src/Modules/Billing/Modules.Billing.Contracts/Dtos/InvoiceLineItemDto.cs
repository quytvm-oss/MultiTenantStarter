using System;
using System.Collections.Generic;
using System.Text;

using Shared.Quota;

namespace Modules.Billing.Contracts.Dtos;

public sealed record InvoiceLineItemDto(
    Guid Id,
    InvoiceLineItemKind Kind,
    QuotaResource? Resource,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount);
