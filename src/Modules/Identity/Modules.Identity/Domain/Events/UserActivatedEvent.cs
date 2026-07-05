using Core.Domain;

namespace Modules.Identity.Domain.Events;

public record UserActivatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    string? ActivatedBy,
    string? CorrelationId = null,
    string? TenantId = null) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static UserActivatedEvent Create(string userId, string? activatedBy = null, string? correlationId = null, string? tenantId = null)
    => new(Guid.CreateVersion7(), DateTimeOffset.UtcNow, userId, activatedBy, correlationId, tenantId);
}