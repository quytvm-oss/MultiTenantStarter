using Mediator;

using Modules.Auditing.Contracts.DTOs;

namespace Modules.Auditing.Contracts.v1.GetAuditsByTrace;

public record GetAuditsByTraceQuery() : IQuery<IReadOnlyList<AuditSummaryDto>>
{
    public string TraceId { get; init; } = default!;

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}