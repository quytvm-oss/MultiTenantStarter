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

    public static InvoiceLineItem Create(
        Guid invoiceId,
        InvoiceLineItemKind kind,
        string description,
        decimal quantity,
        decimal unitPrice,
        string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");
        }

        return new InvoiceLineItem
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = invoiceId,
            Kind = kind,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = new Money(quantity * unitPrice, currency).Round(2)
        };
    }

    internal void AttachResource(QuotaResource resource) => Resource = resource;
}
