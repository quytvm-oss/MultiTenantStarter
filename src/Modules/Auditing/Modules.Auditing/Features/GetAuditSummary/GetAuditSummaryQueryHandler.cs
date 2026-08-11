using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Auditing.Contracts;
using Modules.Auditing.Contracts.Authorization;
using Modules.Auditing.Contracts.DTOs;
using Modules.Auditing.Contracts.v1.GetAuditSummary;
using Modules.Auditing.Persistence;
using Modules.Identity.Contracts.Services;

namespace Modules.Auditing.Features.GetAuditSummary;

public class GetAuditSummaryQueryHandler(
    AuditDbContext dbContext,
    ICurrentUser currentUser,
    IUserPermissionService permissions,
    TimeProvider timeProvider)
    : IQueryHandler<GetAuditSummaryQuery, AuditSummaryAggregateDto>
{
    public static readonly TimeSpan MaxWindow = TimeSpan.FromDays(90);
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromDays(7);

    public async ValueTask<AuditSummaryAggregateDto> Handle(GetAuditSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (fromUtc, toUtc) = ResolveWindow(query.FromUtc, query.ToUtc);
        var baseQuery = await BuildBaseQueryAsync(query, cancellationToken).ConfigureAwait(false);

        var scoped = baseQuery.Where(a => a.OccurredAtUtc >= fromUtc && a.OccurredAtUtc <= toUtc);

        // Four GROUP BYs pushed to SQL against the same filtered set (no materialization).
        // Sequential because they share the DbContext; parallel would need four contexts.
        var byType = await scoped
            .GroupBy(a => a.EventType)
            .Select(g => new { Key = g.Key, Count = (long)g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bySeverity = await scoped
            .GroupBy(a => a.Severity)
            .Select(g => new { Key = g.Key, Count = (long)g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bySource = await scoped
            .Where(a => a.Source != null)
            .GroupBy(a => a.Source!)
            .Select(g => new { Key = g.Key, Count = (long)g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byTenant = await scoped
            .Where(a => a.TenantId != null)
            .GroupBy(a => a.TenantId!)
            .Select(g => new { Key = g.Key, Count = (long)g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AuditSummaryAggregateDto
        {
            EventsByType = byType.ToDictionary(x => (AuditEventType)x.Key, x => x.Count),
            EventsBySeverity = bySeverity.ToDictionary(x => (AuditSeverity)x.Key, x => x.Count),
            EventsBySource = bySource.ToDictionary(x => x.Key, x => x.Count, StringComparer.OrdinalIgnoreCase),
            EventsByTenant = byTenant.ToDictionary(x => x.Key, x => x.Count, StringComparer.OrdinalIgnoreCase),
        };
    }

    /// <summary>
    /// Mirrors <c>GetAuditsQueryHandler.BuildBaseQueryAsync</c>: scoped to the
    /// current tenant by default, opt-in cross-tenant via the explicit
    /// ViewCrossTenant permission.
    /// </summary>
    private async Task<IQueryable<AuditRecord>> BuildBaseQueryAsync(GetAuditSummaryQuery query, CancellationToken ct)
    {
        var currentTenant = currentUser.GetTenantId();
        var requested = string.IsNullOrWhiteSpace(query.TenantId) ? null : query.TenantId;

        bool wantsCrossTenant =
            requested is not null
            && !string.Equals(requested, currentTenant, StringComparison.OrdinalIgnoreCase);

        if (!wantsCrossTenant)
        {
            return dbContext.AuditRecords.AsNoTracking();
        }

        var userId = currentUser.GetUserId().ToString();
        var allowed = await permissions
            .HasPermissionAsync(userId, AuditingPermissions.AuditTrails.ViewCrossTenant, ct)
            .ConfigureAwait(false);
        if (!allowed)
        {
            throw new ForbiddenException("Cross-tenant audit summary requires Permissions.AuditTrails.ViewCrossTenant.");
        }

        return dbContext.AuditRecords
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == requested);
    }

    private (DateTime FromUtc, DateTime ToUtc) ResolveWindow(DateTime? from, DateTime? to)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var resolvedTo = to ?? now;
        var resolvedFrom = from ?? resolvedTo - DefaultWindow;

        if (resolvedTo - resolvedFrom > MaxWindow)
        {
            resolvedFrom = resolvedTo - MaxWindow;
        }

        return (resolvedFrom, resolvedTo);
    }
}