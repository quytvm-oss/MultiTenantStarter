using Core.Domain;
using Core.Domain.ValueObjects;

using Modules.Billing.Contracts;

namespace Modules.Billing.Domain;

public sealed class WalletTransaction : BaseEntity<Guid>
{
    public Guid WalletId { get; private set; }

    public string TenantId { get; private set; } = default!;

    public Money Amount { get; private set; } = default!;

    public WalletTransactionKind Kind { get; private set; }

    public string Description { get; private set; } = default!;

    public string? ReferenceId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private WalletTransaction() { }
}
