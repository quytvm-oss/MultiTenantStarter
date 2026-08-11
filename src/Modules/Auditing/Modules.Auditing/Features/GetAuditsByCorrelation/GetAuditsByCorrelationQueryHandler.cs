using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Auditing.Contracts;
using Modules.Auditing.Contracts.DTOs;
using Modules.Auditing.Contracts.v1.GetAuditsByCorrelation;
using Modules.Auditing.Persistence;

namespace Modules.Auditing.Features.GetAuditsByCorrelation;

public class GetAuditsByCorrelationQueryHandler(AuditDbContext dbContext)
    : IQueryHandler<GetAuditsByCorrelationQuery, IReadOnlyList<AuditSummaryDto>>
{
    public async ValueTask<IReadOnlyList<AuditSummaryDto>> Handle(GetAuditsByCorrelationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<AuditRecord> audits = dbContext.AuditRecords
            .AsNoTracking()
            .Where(x => x.CorrelationId == query.CorrelationId);

        if (query.FromUtc.HasValue)
            audits = audits.Where(a => a.OccurredAtUtc >= query.FromUtc.Value);

        if (query.ToUtc.HasValue)
            audits = audits.Where(a => a.OccurredAtUtc <= query.ToUtc.Value);

        var list = await audits
            .OrderBy(x => x.OccurredAtUtc)
            .Select(a => new AuditSummaryDto()
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
            }).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return list;
    }
}