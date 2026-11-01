using System;
using System.Collections.Generic;
using System.Text;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Billing.Data;
using Modules.Billing.Domain;

using Quota;

using Shared.Multitenancy;
using Shared.Quota;

namespace Modules.Billing.Services;

public sealed class UsageReporter : IUsageReporter
{
    private readonly BillingDbContext _dbContext;

    private readonly IQuotaService _quotas;

    private readonly QuotaPlanResolver _planResolver;

    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;

    private readonly ILogger<UsageReporter> _logger;

    public UsageReporter(
        BillingDbContext dbContext, 
        IQuotaService quotas, 
        QuotaPlanResolver planResolver, 
        IMultiTenantStore<AppTenantInfo> tenantStore, 
        ILogger<UsageReporter> logger)
    {
        _dbContext = dbContext;
        _quotas = quotas;
        _planResolver = planResolver;
        _tenantStore = tenantStore;
        _logger = logger;
    }


    public async Task<IReadOnlyList<UsageSnapshot>> CaptureForPeriodAsync(string tenantId, int periodYear, int periodMonth, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var tenant = await _tenantStore.GetAsync(tenantId).ConfigureAwait(false);

        var existing = await _dbContext.UsageSnapshots
            .Where(s => s.TenantId == tenantId && s.PeriodYear == periodYear && s.PeriodMonth == periodMonth)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var snapshots = new List<UsageSnapshot>(capacity: 4);

        foreach(var resource in Enum.GetValues<QuotaResource>())
        {
            var already = existing.FirstOrDefault(s => s.Resource == resource);

            if (already is not null)
            {
                snapshots.Add(already);
                continue;
            }

            var used = await _quotas.GetCurrentAsync(tenantId, resource, cancellationToken).ConfigureAwait(false);
            var limit = _planResolver.ResolveLimit(tenant, resource);
            var snapshot = UsageSnapshot.Create(tenantId, periodYear, periodMonth, resource, used, limit);
            await _dbContext.UsageSnapshots.AddAsync(snapshot);
            snapshots.Add(snapshot);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Billing] captured {Count} usage snapshots for tenant {TenantId} period {Year}-{Month:00}",
                snapshots.Count, tenantId, periodYear, periodMonth);
        }

        return snapshots;
    }
}
