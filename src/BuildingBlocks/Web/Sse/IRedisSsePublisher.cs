namespace Web.Sse;

public interface IRedisSsePublisher
{
    Task  PublishToUserAsync(string userId, SseEvent sseEvent, CancellationToken ct = default);
    Task  PublishToTenantAsync(string tenantId, SseEvent sseEvent, CancellationToken ct = default);
    Task  PublishToAllAsync(SseEvent sseEvent, CancellationToken ct = default);
}