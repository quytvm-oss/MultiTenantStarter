using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Billing.Contracts.Dtos;

public sealed record WalletTransactionDto(
    Guid Id,
    decimal Amount,
    string Kind,
    string Description,
    string? ReferenceId,
    DateTime CreatedAtUtc);
