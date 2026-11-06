using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Billing.Contracts;
using Modules.Billing.Data;
using Modules.Billing.Domain;
using Modules.Billing.Services;
using Modules.Multitenancy.Contracts.Events;

namespace Modules.Billing.Events;

public sealed class TenantSubscribedIntegrationEventHandler : IConsumer<TenantSubscribedIntegrationEvent>
{
    private readonly BillingDbContext _db;
    private readonly IBillingService _billing;
    private readonly ILogger<TenantSubscribedIntegrationEventHandler> _logger;

    public TenantSubscribedIntegrationEventHandler(
        ILogger<TenantSubscribedIntegrationEventHandler> logger, 
        BillingDbContext db, 
        IBillingService billing)
    {
        _logger = logger;
        _db = db;
        _billing = billing;
    }

    public async Task Consume(ConsumeContext<TenantSubscribedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        var tenantId = message.TenantId
            ?? throw new InvalidOperationException("TenantSubscribedIntegrationEvent is missing TenantId.");

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

        await _billing.CreateSubscriptionInvoiceAsync(
            tenantId, message.PlanId, message.PeriodStartUtc, message.PeriodEndUtc, CancellationToken.None).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[Billing] tenant {TenantId} subscribed to plan {PlanKey}; term ends {End:o}",
                tenantId, message.PlanKey, message.PeriodEndUtc);
        }
    }
}
