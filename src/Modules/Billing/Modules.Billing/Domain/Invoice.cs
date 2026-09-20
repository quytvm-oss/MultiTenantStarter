using Core.Domain;
using Core.Domain.ValueObjects;

using Modules.Billing.Contracts;

namespace Modules.Billing.Domain;

/// <summary>
/// An invoice for a tenant covering a single monthly period. Starts as Draft, transitions to
/// Issued when sent to the customer, then to Paid or Void. Totals are recomputed every time a
/// line is added so callers don't have to.
/// </summary>
public sealed class Invoice : AggregateRoot<Guid>
{
    private readonly List<InvoiceLineItem> _lineItems = new();

    public string TenantId { get; private set; } = default!;

    public string InvoiceNumber { get; private set; } = default!;

    public int PeriodYear { get; private set; }

    public int PeriodMonth { get; private set; }

    /// <summary>
    /// What this invoice bills. <see cref="InvoicePurpose.Subscription"/> covers a plan term (created
    /// on tenant create/renew); <see cref="InvoicePurpose.Usage"/> covers metered overage for a month
    /// (created by the monthly job). The two streams never collide on idempotency keys.
    /// </summary>
    public InvoicePurpose Purpose { get; private set; } = InvoicePurpose.Usage;

    /// <summary>Start of the billed term (subscription invoices only).</summary>
    public DateTime? PeriodStartUtc { get; private set; }

    /// <summary>End of the billed term (subscription invoices only).</summary>
    public DateTime? PeriodEndUtc { get; private set; }

    public Money SubtotalAmount { get; private set; } = default!;

    public string Currency => SubtotalAmount.Currency;

    public InvoiceStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? IssuedAtUtc { get; private set; }

    public DateTime? DueAtUtc { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public DateTime? VoidedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems;

    private Invoice() { }
}
