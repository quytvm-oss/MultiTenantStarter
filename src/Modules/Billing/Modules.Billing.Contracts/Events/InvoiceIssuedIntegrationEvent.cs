using System;
using System.Collections.Generic;
using System.Text;

using Core.Messaging;

namespace Modules.Billing.Contracts.Events;

public sealed record InvoiceIssuedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    DateTime? DueAtUtc,
    int PeriodYear,
    int PeriodMonth
    ) : IIntegrationEvent;
