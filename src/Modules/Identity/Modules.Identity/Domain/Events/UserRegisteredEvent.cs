using Core.Domain;

namespace Modules.Identity.Domain.Events;

public sealed record UserRegisteredEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? CorrelationId = null,
    string? TenantId = null,
    string? Source = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId)
{
    public static UserRegisteredEvent Create(string userId, string email, string? firstName = null, string? lastName = null, string? correlationId = null, string? tenantId = null, string? source = null)
        => new(Guid.CreateVersion7(), DateTimeOffset.UtcNow, userId, email, firstName, lastName, correlationId, tenantId, source);
}