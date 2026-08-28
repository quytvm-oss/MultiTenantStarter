using System.Collections.Concurrent;

using Finbuckle.MultiTenant.Abstractions;

using Shared.Multitenancy;

using Shared.Quota;

namespace Quota;

public class InMemoryQuotaService : IQuotaService
{
    private readonly ConcurrentDictionary<string, long> _counters;
    private readonly QuotaOptions _options;
    private readonly QuotaPlanResolver _planResolver;
    private readonly IMultiTenantContextAccessor<AppTenantInfo>? _tenantAccessor;
    private readonly Dictionary<QuotaResource, IQuotaGaugeProvider> _gauges;
    private readonly TimeProvider _timeProvider;

    public InMemoryQuotaService(
        InMemoryQuotaStore store,
        QuotaOptions options,
        QuotaPlanResolver planResolver,
        IEnumerable<IQuotaGaugeProvider> gauges,
        TimeProvider timeProvider,
        IMultiTenantContextAccessor<AppTenantInfo>? tenantAccessor = null)
    {

        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(planResolver);
        ArgumentNullException.ThrowIfNull(gauges);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _counters = store.Counters;
        _options = options;
        _planResolver = planResolver;
        _tenantAccessor = tenantAccessor;
        _gauges = gauges.ToDictionary(g => g.Resource);
        _timeProvider = timeProvider;
    }

    public ValueTask<QuotaCheckResult> CheckAndRecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<QuotaCheckResult> CheckAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var (limit, exempt) = ResolveLimit(tenantId, resource);

        var current = GetCounter(tenantId, resource);

        if (exempt || limit == long.MaxValue)
        {
            return ValueTask.FromResult(QuotaCheckResult.Unlimited(resource, current));
        }

        var allowed = limit + current <= limit;

        return ValueTask.FromResult(new QuotaCheckResult(allowed, resource, current, limit, GetPeriodResetUtc(resource)));
    }



    public ValueTask<long> GetCurrentAsync(string tenantId, QuotaResource resource, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<long> RecordAsync(string tenantId, QuotaResource resource, long amount, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    #region

    private (long limit, bool exempt) ResolveLimit(string tenantId, QuotaResource resource)
    {
        if (_options.ExemptRootTenant && string.Equals(tenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
        {
            return (long.MaxValue, true);
        }

        var tenant = _tenantAccessor?.MultiTenantContext?.TenantInfo;

        if (tenant is not null && !string.Equals(tenant.Id, tenantId, StringComparison.Ordinal))
        {
            tenant = null;
        }

        return (_planResolver.ResolveLimit(tenant, resource), false);
    }

    private long GetCounter(string tenantId, QuotaResource resource)
    {
        return _counters.TryGetValue(GetCounterKey(tenantId, resource), out var value) ? value : 0;
    }

    private static bool IsPeriodic(QuotaResource resource) => resource switch
    {
        QuotaResource.ApiCalls => true,
        _ => false
    };

    private string GetCounterKey(string tenantId, QuotaResource resource)
    {
        if (!IsPeriodic(resource))
        {
            return $"quota:{tenantId}:{resource}";
        }

        var now = _timeProvider.GetUtcNow();
        var period = $"{now.Year:D4}{now.Month:D2}";
        return $"quota:{tenantId}:{resource}:{period}";
    }

    private DateTimeOffset? GetPeriodResetUtc(QuotaResource resource)
    {
        if (!IsPeriodic(resource))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        return now.Month == 12
            ? new DateTimeOffset(now.Year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(now.Year, now.Month + 1, 1, 0, 0, 0, TimeSpan.Zero);
    }



    #endregion
}
