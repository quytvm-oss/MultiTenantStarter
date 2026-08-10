using System.Text.Json;

using Mediator;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;
using Modules.Auditing.Contracts.DTOs;
using Modules.Auditing.Contracts.v1.GetAuditById;
using Modules.Auditing.Persistence;

namespace Modules.Auditing.Features.GetAuditById;

public class GetAuditByIdHandler(AuditDbContext dbContext, ILogger<GetAuditByIdHandler> logger)
    : IQueryHandler<GetAuditByIdQuery, AuditDetailDto>
{
    
    public async ValueTask<AuditDetailDto> Handle(GetAuditByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        
        var record = await dbContext.AuditRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        if (record is null)
        {
            // KeyNotFoundException maps to 404 globally. Kept (not framework NotFoundException)
            // because audit exception-type fixtures and severity classification key off this type.
            throw new KeyNotFoundException($"Audit record {query.Id} not found.");
        }

        JsonElement payload;

        try
        {
            using var document = JsonDocument.Parse(record.PayloadJson);
            payload = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse audit payload JSON for record {AuditId}.", query.Id);
            payload = JsonDocument.Parse("{}").RootElement.Clone();
        }

        return new AuditDetailDto()
        {
            Id = record.Id,
            OccurredAtUtc = record.OccurredAtUtc,
            ReceivedAtUtc = record.ReceivedAtUtc,
            EventType = (AuditEventType)record.EventType,
            Severity = (AuditSeverity)record.Severity,
            TenantId = record.TenantId,
            UserId = record.UserId,
            UserName = record.UserName,
            TraceId = record.TraceId,
            SpanId = record.SpanId,
            CorrelationId = record.CorrelationId,
            RequestId = record.RequestId,
            Source = record.Source,
            Tags = (AuditTag)record.Tags,
            Payload = payload
        };
    }
}