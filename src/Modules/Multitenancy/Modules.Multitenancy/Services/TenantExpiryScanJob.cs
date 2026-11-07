using System;
using System.Collections.Generic;
using System.Text;

using Core.Messaging;

using Finbuckle.MultiTenant.Abstractions;

using MassTransit;

using Messaging.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Modules.Multitenancy.Contracts.Events;
using Modules.Multitenancy.Data;
using Modules.Multitenancy.Domain;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Services;

public sealed class TenantExpiryScanJob
{
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;


    private readonly TenantDbContext _db;

    private readonly IPublishEndpoint _bus;

    private readonly IMultiTenantContextSetter _tenantContextSetter;

    private readonly TimeProvider _timeProvider;

    private readonly TenantBillingOptions _options;

    private readonly MessagingDbContext _messagingDb;

    private readonly ILogger<TenantExpiryScanJob> _logger;

    public TenantExpiryScanJob(
        IMultiTenantStore<AppTenantInfo> tenantStore, 
        TenantDbContext db, 
        IPublishEndpoint bus, 
        IMultiTenantContextSetter tenantContextSetter, 
        TimeProvider timeProvider,
        IOptions<TenantBillingOptions> options, 
        MessagingDbContext messagingDb, 
        ILogger<TenantExpiryScanJob> logger)
    {
        _tenantStore = tenantStore;
        _db = db;
        _bus = bus;
        _tenantContextSetter = tenantContextSetter;
        _timeProvider = timeProvider;
        _options = options.Value;
        _messagingDb = messagingDb;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tenants = await _tenantStore.GetAllAsync().ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var published = 0;
        foreach (var tenant in tenants)
        {
            if (!tenant.IsActive || string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                if (await TryNotifyAsync(tenant, now, cancellationToken).ConfigureAwait(false))
                {
                    published++;
                }
            }
#pragma warning disable CA1031 // One tenant's failure must not block the rest of the scan
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "[Multitenancy] expiry scan failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Multitenancy] expiry scan published {Count} notice(s)", published);
        }

    }

    private async Task<bool> TryNotifyAsync(AppTenantInfo tenant, DateTime now, CancellationToken cancellationToken)
    {
        var validUpTo = tenant.ValidUpTo;
        var graceEnds = validUpTo.AddDays(_options.GracePeriodDays);

        string noticeType;
        if (now > graceEnds)
        {
            noticeType = TenantExpiryNoticeTypes.Expired;
        }
        else if (now > validUpTo)
        {
            noticeType = TenantExpiryNoticeTypes.EnteredGrace;
        }
        else if (now >= validUpTo.AddDays(-_options.ExpiryNotificationLeadDays))
        {
            noticeType = TenantExpiryNoticeTypes.NearingExpiry;
        }
        else
        {
            return false; // healthy and outside the reminder window
        }

        // Dedup: one notice per tenant per state per validity period (re-arms when ValidUpto changes).
        var alreadyNotified = await _db.TenantExpiryNotices
            .AnyAsync(x => x.TenantId == tenant.Id && x.NoticeType == noticeType && x.ValidUptoUtc == validUpTo, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyNotified)
        {
            return false;
        }

        await _db.TenantExpiryNotices.AddAsync(TenantExpiryNotice.Record(tenant.Id, noticeType, validUpTo, now));
        await _db.SaveChangesAsync(cancellationToken);

        // publish event
        await _bus.Publish(BuildEvent(noticeType, tenant, validUpTo, graceEnds, now), cancellationToken)
             .ConfigureAwait(false);
        await _messagingDb.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IIntegrationEvent BuildEvent(
        string noticeType, AppTenantInfo tenant, DateTime validUpto, DateTime graceEnds, DateTime now)
    {
        var id = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7().ToString();
        const string source = "Multitenancy";
        var name = tenant.Name ?? tenant.Id;
        var email = tenant.AdminEmail;

        return noticeType switch
        {
            TenantExpiryNoticeTypes.NearingExpiry => new TenantNearingExpiryIntegrationEvent(
                id, now, tenant.Id, correlationId, source, name, email, tenant.Plan, validUpto, graceEnds,
                DaysRemaining: Math.Max(0, (int)Math.Ceiling((validUpto - now).TotalDays))),
            TenantExpiryNoticeTypes.EnteredGrace => new TenantEnteredGraceIntegrationEvent(
                id, now, tenant.Id, correlationId, source, name, email, tenant.Plan, validUpto, graceEnds),
            _ => new TenantExpiredIntegrationEvent(
                id, now, tenant.Id, correlationId, source, name, email, tenant.Plan, validUpto, graceEnds),
        };
    }
}
