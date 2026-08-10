using Mediator;

using Modules.Auditing.Contracts.DTOs;

namespace Modules.Auditing.Contracts.v1.GetAuditSummary;

public record GetAuditSummaryQuery() : IQuery<AuditSummaryAggregateDto>
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? TenantId { get; init; }
};