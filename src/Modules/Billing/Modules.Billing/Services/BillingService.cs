using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Billing.Contracts;
using Modules.Billing.Contracts.Events;
using Modules.Billing.Data;
using Modules.Billing.Domain;

using Rebus.Bus;

using Shared.Multitenancy;

namespace Modules.Billing.Services;

public sealed class BillingService : IBillingService
{
    private readonly BillingDbContext _dbContext;


    private readonly IUsageReporter _usageReporter;

    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

    private readonly IBus _bus;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<BillingService> _logger;

    public BillingService(
        BillingDbContext dbContext,
        IUsageReporter usageReporter,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IBus bus,
        TimeProvider timeProvider,
        ILogger<BillingService> logger,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    {
        _dbContext = dbContext;
        _usageReporter = usageReporter;
        _tenantStore = tenantStore;
        _bus = bus;
        _timeProvider = timeProvider;
        _logger = logger;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<Invoice?> CreateSubscriptionInvoiceAsync(string tenantId, Guid planId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);


        throw new NotImplementedException();
    }

    public async Task<Invoice> CreateTopupInvoiceAsync(string tenantId, Guid topupRequestId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var request = await _dbContext.TopupRequests
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == topupRequestId && x.Status == TopupRequestStatus.Pending, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Top-up request {topupRequestId} not found or not pending.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var invoiceNumber = BuildTopupInvoiceNumber(tenantId, now, topupRequestId);

        var invoice = Invoice.CreateInvoiceDraft(
            tenantId,
            invoiceNumber,
            now.Year,
            now.Month,
            request.Amount.Currency,
            request.Amount.Amount,
            $"WhatsApp wallet top-up ({request.Amount.Amount:0.##} {request.Amount.Currency})");

        invoice.Issue();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[Billing] issued top-up invoice {InvoiceNumber} for tenant {TenantId} amount={Amount} {Currency}",
                invoice.InvoiceNumber, tenantId, invoice.SubtotalAmount.Amount, invoice.Currency);
        }

        await _bus.Publish(new InvoiceIssuedIntegrationEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: now,
            TenantId: tenantId,
            CorrelationId: Guid.CreateVersion7().ToString(),
            Source: "Billing",
            InvoiceId: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            Amount: invoice.SubtotalAmount.Amount,
            Currency: invoice.Currency,
            DueAtUtc: invoice.DueAtUtc,
            PeriodYear: invoice.PeriodYear,
            PeriodMonth: invoice.PeriodMonth
            )).ConfigureAwait(false);

        return invoice;
    }

    public async Task<Invoice?> GenerateInvoiceForPeriodAsync(string tenantId, int periodYear, int periodMonth, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // Scope the idempotency check to Purpose==Usage: a Subscription invoice may legitimately share
        // the month, and without this filter we'd match it and skip the usage/overage invoice (unbilled overage).
        var existing = await _dbContext.Invoices.
            FirstOrDefaultAsync(i => i.TenantId == tenantId && i.PeriodYear == periodYear && i.PeriodMonth == periodMonth
            && i.Purpose == InvoicePurpose.Usage, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[Billing] usage invoice already exists for tenant {TenantId} period {Year}-{Month:00}, skipping",
                    tenantId, periodYear, periodMonth);
            }
            return existing;
        }

        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            _logger.LogWarning("[Billing] no active subscription for tenant {TenantId}, skipping invoice", tenantId);
            return null;
        }

        var plan = await _dbContext.Plans.FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException($"Plan {subscription.PlanId} not found for tenant {tenantId}.");

        var snapshots = await _usageReporter.CaptureForPeriodAsync(tenantId, periodYear, periodMonth, cancellationToken)
            .ConfigureAwait(false);


        // Usage invoices bill metered overage only. The plan's base fee is billed by the
        // subscription invoice on tenant create/renew (see CreateSubscriptionInvoiceAsync), so it is
        // intentionally NOT added here — otherwise monthly plans would be double-billed.
        var invoiceNumber = BuildUsageInvoiceNumber(tenantId, periodYear, periodMonth);
        var invoice = Invoice.CreateDraft(tenantId, invoiceNumber, periodYear, periodMonth, plan.Currency,
            InvoicePurpose.Usage, periodStartUtc: null, periodEndUtc: null);

        foreach (var snap in snapshots)
        {
            if (snap.Overage <= 0)
            {
                continue;
            }

            var rate = plan.GetOverageRate(snap.Resource);

            if (rate <= 0)
            {
                continue;
            }

            var line = invoice.AddLineItem(InvoiceLineItemKind.Overage,
                $"{snap.Resource} overage ({snap.Overage} units)",
                snap.Overage,
                rate
                );
            line.AttachResource(snap.Resource);
        }

        await _dbContext.Invoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Billing] generated draft invoice {InvoiceNumber} for tenant {TenantId} period {Year}-{Month:00} total={Total} {Currency}",
                invoice.InvoiceNumber, tenantId, periodYear, periodMonth, invoice.SubtotalAmount.Amount, invoice.Currency);
        }

        return invoice;
    }

    public async Task<int> GenerateInvoicesForAllTenantsAsync(int periodYear, int periodMonth, CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantStore.GetAllAsync().ConfigureAwait(false);
        var activeTenantIds = tenants.Where(x => x.IsActive).Select(x => x.Id).ToList();
        var subscribedTenantIds = await _dbContext.Subscriptions
            .Where(x => x.Status == SubscriptionStatus.Active && activeTenantIds.Contains(x.TenantId))
            .Select( x => x.TenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var alreadyInvoiced = await _dbContext.Invoices
            .Where(i => i.PeriodYear == periodYear && i.PeriodMonth == periodMonth &&
                i.Purpose == InvoicePurpose.Usage && subscribedTenantIds.Contains(i.TenantId))
            .Select(i => i.TenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var toGenerate = subscribedTenantIds.Except(alreadyInvoiced, StringComparer.Ordinal).ToList();

        var generated = 0;
        foreach (var tenantId in toGenerate)
        {

            try
            {
                var inv = await GenerateInvoiceForPeriodAsync(tenantId, periodYear, periodMonth, cancellationToken).ConfigureAwait(false);
                if (inv is not null)
                {
                    generated++;
                }
            }
#pragma warning disable CA1031 // One tenant's failure must not block the others
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "[Billing] failed generating invoice for tenant {TenantId} period {Year}-{Month:00}",
                    tenantId, periodYear, periodMonth);
            }
        }

        return generated;
    }

    public async Task<Wallet> GetOrCreateWalletAsync(string tenantId, string currency, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var wallet = await _dbContext.Wallets.Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

        if (wallet is null)
        {
            wallet = Wallet.Create(tenantId, currency);
            await _dbContext.AddAsync(wallet);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return wallet;
    }

    public async Task IssueInvoiceAsync(Guid invoiceId, DateTime? dueAtUtc, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.Issue(dueAtUtc);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkInvoicePaidAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.MarkPaid();

        // When a top-up invoice is paid, credit the tenant's wallet and complete the request —
        // all in the same SaveChanges so the credit + status flip are atomic.
        if (invoice.Purpose == InvoicePurpose.Topup)
        {
            var topupRequest = await _dbContext.TopupRequests
                .FirstOrDefaultAsync(x => x.InvoiceId == invoice.Id, cancellationToken)
                .ConfigureAwait(false);

            if (topupRequest is { Status: TopupRequestStatus.Invoiced })
            {
                var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.TenantId == invoice.TenantId, cancellationToken)
                   .ConfigureAwait(false);

                if (wallet is null)
                {
                    wallet = Wallet.Create(invoice.TenantId, invoice.Currency);
                    await _dbContext.Wallets.AddAsync(wallet);
                }
                else if (!string.Equals(wallet.Currency, invoice.Currency, StringComparison.Ordinal))
                {
                    throw new CustomException(
                        $"Cannot credit a {invoice.Currency} top-up to a {wallet.Currency} wallet.",
                        errors: null,
                        HttpStatusCode.Conflict);
                }

                wallet.Credit(
                    invoice.SubtotalAmount,
                    WalletTransactionKind.Topup,
                    "WhatsApp wallet top-up",
                    topupRequest.Id.ToString());

                topupRequest.MarkCompleted();
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task VoidInvoiceAsync(Guid invoiceId, string? reason, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(false);
        invoice.Void(reason);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }


    #region private mothod

    /// <summary>
    /// Generates a collision-safe invoice number for a top-up.
    /// Format: <c>TOP-{yyyyMM}-{tenantToken}-{requestSuffix}</c>
    /// where <c>requestSuffix</c> is 8 hex chars from the last 4 bytes of <paramref name="topupRequestId"/>.
    /// Each <see cref="TopupRequest"/> has a unique <see cref="Guid"/>, so two top-ups for the same
    /// tenant in the same month produce distinct numbers and never collide on the unique InvoiceNumber index.
    /// </summary>
    private static string BuildTopupInvoiceNumber(string tenantId, DateTime now, Guid topupRequestId)
    {
        var suffix = Convert.ToHexString(topupRequestId.ToByteArray(), 12, 4);
        return $"TOP-{now:yyyyMM}-{TenantToken(tenantId)}-{suffix}";
    }

    // Stable, collision-resistant token from the full tenant id; a naive prefix truncation would
    // collide for shared-prefix tenants and clash on the unique InvoiceNumber index.
    private static string TenantToken(string tenantId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId));
        return Convert.ToHexString(hash, 0, 6);
    }

    private static string BuildUsageInvoiceNumber(string tenantId, int periodYear, int periodMonth) =>
         $"USG-{periodYear}{periodMonth:00}-{TenantToken(tenantId)}";

    private static string BuildSubscriptionInvoiceNumber(string tenantId, DateTime periodStartUtc) =>
        $"SUB-{periodStartUtc:yyyyMM}-{TenantToken(tenantId)}";

    private async Task<Invoice> LoadInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var callerTenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id
           ?? throw new UnauthorizedException("Tenant context is required.");
        var isRoot = callerTenantId == MultitenancyConstants.Root.Id;

        return await _dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && (isRoot || i.TenantId == callerTenantId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {invoiceId} not found.");
    }

    #endregion
}
