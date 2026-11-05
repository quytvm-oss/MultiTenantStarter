using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Billing.Contracts;
using Modules.Billing.Data;
using Modules.Billing.Domain;
using Modules.Billing.Services;
using Modules.Multitenancy.Contracts.Events;

using Rebus.Handlers;

namespace Modules.Identity.Events;

public sealed class TenantRenewedIntegrationEventHandler : IHandleMessages<TenantRenewedIntegrationEvent>
{
    private readonly BillingDbContext _db;
    private readonly IBillingService _billing;
    private readonly ILogger<TenantRenewedIntegrationEventHandler> _logger;

    public TenantRenewedIntegrationEventHandler(
        BillingDbContext db, 
        IBillingService billing, 
        ILogger<TenantRenewedIntegrationEventHandler> logger)
    {
        _db = db;
        _billing = billing;
        _logger = logger;
    }

    public async Task Handle(TenantRenewedIntegrationEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var tenantId = message.TenantId
            ?? throw new InvalidOperationException("TenantRenewedIntegrationEvent is missing TenantId.");

        if (message.PlanChanged)
        {
            var active = await _db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId 
                        && s.Status == SubscriptionStatus.Active, CancellationToken.None)
             .ConfigureAwait(false);
            active?.Cancel(message.PeriodStartUtc);

            await _db.Subscriptions.AddAsync(
                Subscription.Create(tenantId, 
                    message.PlanId, 
                    message.PeriodStartUtc, 
                    message.PeriodEndUtc), CancellationToken.None);
            await _db.SaveChangesAsync(CancellationToken.None);
        }
        else
        {
            // Same-plan renewal: extend the active subscription's term so EndUtc tracks the renewed
            // ValidUpto (otherwise the dashboard's "Current term"/validity drifts behind enforcement).
            var active = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, CancellationToken.None)
            .ConfigureAwait(false);
            if (active is null)
            {
                return;
            }

            active.Extend(message.PeriodEndUtc);
            await _db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }

        await _billing.CreateSubscriptionInvoiceAsync(
           tenantId, message.PlanId, message.PeriodStartUtc, message.PeriodEndUtc, CancellationToken.None).ConfigureAwait(false);


        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[Billing] tenant {TenantId} renewed on plan {PlanKey} (planChanged={PlanChanged}); term ends {End:o}",
                tenantId, message.PlanKey, message.PlanChanged, message.PeriodEndUtc);
        }
    }
}
