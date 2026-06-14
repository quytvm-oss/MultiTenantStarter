namespace Web.Sse;

public sealed record SseEvent(
    string EventType,
    string Data,
    string? Id = null,
    DateTimeOffset? Timestamp = null);