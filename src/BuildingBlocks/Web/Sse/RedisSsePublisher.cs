using System.Text.Json;

using StackExchange.Redis;

namespace Web.Sse;

internal sealed class RedisSsePublisher(IConnectionMultiplexer redis) : IRedisSsePublisher
{
    private ISubscriber Sub => redis.GetSubscriber();

    public Task  PublishToUserAsync(string userId, SseEvent evt, CancellationToken ct = default) =>
        Sub.PublishAsync(
            RedisChannel.Literal("sse:user"),
            Serialize(new TargetedEnvelope(userId, Stamp(evt))));

    public Task  PublishToTenantAsync(string tenantId, SseEvent evt, CancellationToken ct = default) =>
        Sub.PublishAsync(
            RedisChannel.Literal("sse:tenant"),
            Serialize(new TargetedEnvelope(tenantId, Stamp(evt))));

    public Task  PublishToAllAsync(SseEvent evt, CancellationToken ct = default) =>
        Sub.PublishAsync(
            RedisChannel.Literal("sse:all"),
            Serialize(Stamp(evt)));

    private static SseEvent Stamp(SseEvent e) =>
        e with { Timestamp = e.Timestamp ?? DateTimeOffset.UtcNow };

    private static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj);

    private sealed record TargetedEnvelope(string TargetId, SseEvent Event);
}