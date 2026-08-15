using Core.Messaging;


namespace Modules.Multitenancy.Contracts.Events;

public sealed record TenantSubscribedIntegrationEvent(
    string? TenantId,
    string CorrelationId,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc)
    : IIntegrationEvent;