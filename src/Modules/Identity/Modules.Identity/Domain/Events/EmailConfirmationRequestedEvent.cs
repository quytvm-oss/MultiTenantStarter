using Core.Domain;

namespace Modules.Identity.Domain.Events;

public sealed record EmailConfirmationRequestedEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    string Email,
    string Origin ,
    string TenantId,
    string? CorrelationId = null
) : DomainEvent(EventId, OccurredOnUtc,CorrelationId, TenantId)
{
    public static EmailConfirmationRequestedEvent Create(string userId, string email,string origin, string tenantId)
        => new(Guid.CreateVersion7(), DateTimeOffset.UtcNow, userId, email, origin, tenantId);
}