using Core.Messaging;

namespace Shared.Webhooks;

public sealed record WebhookEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    string EventType,
    string FullName,
    string Payload) : IIntegrationEvent;
