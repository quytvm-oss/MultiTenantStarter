using Core.Messaging;


namespace Modules.Multitenancy.Contracts.Events;

public sealed record TenantSubscribedIntegrationEvent(
    Guid Id,
    string? TenantId,
    string CorrelationId,
    DateTime OccurredOnUtc,
    string Source,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc)
    : IIntegrationEvent;