using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Auditing.Contracts;
using Modules.Auditing.Contracts.Authorization;
using Modules.Auditing.Contracts.DTOs;
using Modules.Auditing.Contracts.v1.GetAudits;
using Modules.Auditing.Persistence;

using static Modules.Auditing.Persistence.AuditJsonbFunctions;
using Modules.Identity.Contracts.Services;

using Persistence.Pagination;

using Shared.Persistence;

namespace Modules.Auditing.Features.GetAudits;

public class GetAuditsQueryHandler(AuditDbContext dbContext, ICurrentUser currentUser, 
    IUserPermissionService permissions, TimeProvider timeProvider)
    : IQueryHandler<GetAuditsQuery, PagedResponse<AuditSummaryDto>>
{
    /// <summary>
    /// Maximum window allowed when the caller supplies a from/to. We refuse
    /// to scan the entire table — without this guard, an unconstrained query
    /// degenerates into a full sequential scan as the audit volume grows.
    /// </summary>
    public static readonly TimeSpan MaxWindow = TimeSpan.FromDays(90);
    /// <summary>
    /// Default lookback when the caller does not supply a from/to. Keeps the
    /// happy-path query bounded for the dashboard.
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromDays(7);

    public async ValueTask<PagedResponse<AuditSummaryDto>> Handle(GetAuditsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (fromUtc, toUtc) = ResolveWindow(query.FromUtc, query.ToUtc);
        
        var audits = await BuildBaseQueryAsync(query, cancellationToken).ConfigureAwait(false);

        audits = audits.Where(a => a.OccurredAtUtc >= fromUtc && a.OccurredAtUtc <= toUtc);

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            audits = audits.Where(a => a.UserId == query.UserId);
        }

        if (query.EventType.HasValue)
        {
            audits = audits.Where(a => a.EventType == (int)query.EventType.Value);
        }

        if (query.ExcludeEventType.HasValue)
        {
            audits = audits.Where(a => a.EventType != (int)query.ExcludeEventType.Value);
        }

        if (query.Severity.HasValue)
        {
            audits = audits.Where(a => a.Severity == (byte)query.Severity.Value);
        }

        if (query.Tags.HasValue && query.Tags.Value != AuditTag.None)
        {
            long tagMask = (long)query.Tags.Value;
            audits = audits.Where(a => (a.Tags & tagMask) != 0);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            audits = audits.Where(a => a.Source == query.Source);
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            audits = audits.Where(a => a.CorrelationId == query.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(query.TraceId))
        {
            audits = audits.Where(a => a.TraceId == query.TraceId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search;
            // ILIKE on PayloadJson is sequential; the (TenantId, OccurredAtUtc) index
            // scopes the scan — add a GIN index on PayloadJson in prod for fast search.
            audits = audits.Where(a =>
                (a.PayloadJson != null && EF.Functions.ILike(AsText(a.PayloadJson), $"%{term}%")) ||
                (a.Source != null && EF.Functions.ILike(a.Source, $"%{term}%")) ||
                (a.UserName != null && EF.Functions.ILike(a.UserName, $"%{term}%")));
        }

        audits = audits.OrderByDescending(a => a.OccurredAtUtc);

        IQueryable<AuditSummaryDto> projected = audits.Select(a => new AuditSummaryDto
        {
            Id = a.Id,
            OccurredAtUtc = a.OccurredAtUtc,
            EventType = (AuditEventType)a.EventType,
            Severity = (AuditSeverity)a.Severity,
            TenantId = a.TenantId,
            UserId = a.UserId,
            UserName = a.UserName,
            TraceId = a.TraceId,
            CorrelationId = a.CorrelationId,
            RequestId = a.RequestId,
            Source = a.Source,
            Tags = (AuditTag)a.Tags
        });

        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
        
    }
    
    /// <summary>
    /// Returns a queryable already scoped to the right tenant. If the caller
    /// supplied a TenantId equal to their own, that's a no-op. Cross-tenant
    /// access requires the explicit ViewCrossTenant permission and bypasses
    /// Finbuckle's anonymous tenant filter, then re-applies an explicit
    /// TenantId predicate so we never accidentally return rows for *all*
    /// tenants.
    /// </summary>
    private async Task<IQueryable<AuditRecord>> BuildBaseQueryAsync(GetAuditsQuery query, CancellationToken ct)
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
            throw new ForbiddenException("Cross-tenant audit access requires Permissions.AuditTrails.ViewCrossTenant.");
        }

        return dbContext.AuditRecords
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == requested);
    }

    /// <summary>
    /// Clamps the supplied window to <see cref="MaxWindow"/> and supplies a
    /// <see cref="DefaultWindow"/> when both endpoints are missing. The
    /// validator catches obvious misuse (from &gt; to); this method handles
    /// the open-ended "no range" case so the SQL is always bounded.
    /// </summary>
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