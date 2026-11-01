using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Billing.Contracts.Dtos;

using Shared.Persistence;

namespace Modules.Billing.Contracts.v1.Invoices;

public sealed record GetInvoicesQuery(
    string? TenantId = null,
    InvoiceStatus? Status = null,
    int? PeriodYear = null,
    int? PeriodMonth = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResponse<InvoiceDto>>;
