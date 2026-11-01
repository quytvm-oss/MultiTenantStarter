using System;
using System.Collections.Generic;
using System.Text;

using Modules.Billing.Contracts.Dtos;
using Modules.Billing.Domain;

namespace Modules.Billing.Mappings;

internal static class WalletMappings
{
    public static WalletDto ToDo(this Wallet w, int recentCount = 10)
        => new WalletDto(w.Id, w.TenantId, w.Currency, w.Balance.Amount, w.Status.ToString(), w.CreatedAtUtc,
            w.Transactions
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(recentCount)
            .Select(t => t.ToDo())
            .ToList());

    public static WalletTransactionDto ToDo(this WalletTransaction wt)
        => new WalletTransactionDto(wt.Id, wt.Amount.Amount, wt.Kind.ToString(), wt.Description, wt.ReferenceId, wt.CreatedAtUtc);

    public static TopupRequestDto ToDo(this TopupRequest tr)
        => new TopupRequestDto(tr.Id, tr.TenantId, tr.Amount.Amount, tr.Amount.Currency, tr.Note, tr.Status.ToString(),
            tr.InvoiceId, tr.RequestedBy, tr.DecisionNote, tr.CreatedAtUtc, tr.DecidedAtUtc, tr.CompletedAtUtc);

  
}
