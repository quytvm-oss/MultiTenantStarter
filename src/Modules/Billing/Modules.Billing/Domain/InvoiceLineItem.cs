using Core.Domain;
using Core.Domain.ValueObjects;

using Modules.Billing.Contracts;

using Shared.Quota;

namespace Modules.Billing.Domain;

public sealed class InvoiceLineItem : BaseEntity<Guid>
{
    public Guid InvoiceId { get; set; }

    public InvoiceLineItemKind Kind { get; private set; }

    public QuotaResource? Resource { get; private set; }

    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Money Amount { get; private set; } = default!;

    private InvoiceLineItem() { }
}
