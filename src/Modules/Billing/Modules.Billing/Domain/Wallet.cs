using Core.Domain;
using Core.Domain.ValueObjects;

using Modules.Billing.Contracts;

namespace Modules.Billing.Domain;

public sealed class Wallet : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = default!;

    public Money Balance { get; private set; } = default!;

    public string Currency => Balance.Currency;

    public WalletStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<WalletTransaction> _transactions = new();

    public IReadOnlyList<WalletTransaction> Transactions => _transactions;

    private Wallet() { }
}
