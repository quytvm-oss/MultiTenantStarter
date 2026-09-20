using Core.Domain;
using Core.Domain.ValueObjects;

using Modules.Billing.Contracts;

namespace Modules.Billing.Domain;

public sealed class TopupRequest : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = default!;

    public Money Amount { get; private set; } = default!;

    public string? Note { get; private set; }

    public TopupRequestStatus Status { get; private set; }

    public Guid? InvoiceId { get; private set; }

    public string? RequestedBy { get; private set; }

    public string? DecisionNote { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    private TopupRequest() { }
}
