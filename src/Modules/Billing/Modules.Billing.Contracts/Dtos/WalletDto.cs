using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Billing.Contracts.Dtos;

public sealed record WalletDto(
    Guid Id,
    string TenantId,
    string Currency,
    decimal Balance,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<WalletTransactionDto> RecentTransactions);
