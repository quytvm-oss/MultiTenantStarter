namespace Core.Messaging;

public interface IIntegrationEvent
{
    Guid Id { get; init; }

    DateTime OccurredOnUtc { get; init; }

    /// <summary>
    /// Tenant identifier for tenant-scoped events. Null for global events.
    /// </summary>
    string? TenantId { get; init; }

    /// <summary>
    /// Correlation identifier to tie events to requests and traces.
    /// </summary>
    string CorrelationId { get; init; }

    /// <summary>
    /// Logical source of the event (e.g., module or service name).
    /// </summary>
    string Source { get; init; }
}