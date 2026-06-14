using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Web.Sse;

public sealed class SseConnectionManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, Connection> _connections = new();
    private readonly ISubscriber _subscriber;
    private readonly ILogger<SseConnectionManager> _logger;

    public SseConnectionManager(IConnectionMultiplexer redis, ILogger<SseConnectionManager> logger)
    {
        _subscriber = redis.GetSubscriber();
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _subscriber.SubscribeAsync(
            RedisChannel.Literal("sse:user"),
            OnUserMessage).ConfigureAwait(false);

        await _subscriber.SubscribeAsync(
            RedisChannel.Literal("sse:tenant"),
            OnTenantMessage).ConfigureAwait(false);

        await _subscriber.SubscribeAsync(
            RedisChannel.Literal("sse:all"),
            OnBroadcastMessage).ConfigureAwait(false);
    }

    public (Guid ConnectionId, ChannelReader<SseEvent> Reader) Connect(
        string userId, string? tenantId = null)
    {
        var connectionId = Guid.CreateVersion7();
        var channel = Channel.CreateBounded<SseEvent>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        _connections[connectionId] = new Connection(userId, tenantId, channel);

        _logger.LogDebug("SSE connected: {ConnectionId} user={UserId} tenant={TenantId}",
            connectionId, userId, tenantId ?? "none");

        return (connectionId, channel.Reader);
    }

    public void Disconnect(Guid connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var conn)) return;
        conn.Channel.Writer.TryComplete();

        _logger.LogDebug("SSE disconnected: {ConnectionId}", connectionId);
    }

    // ── Redis handlers ──

    private void OnUserMessage(RedisChannel _, RedisValue value)
    {
        if (!TryDeserialize<TargetedEnvelope>(value, out var envelope) || envelope is null) return;

        foreach (var (_, conn) in _connections)
            if (conn.UserId == envelope.TargetId)
                conn.Channel.Writer.TryWrite(envelope.Event);
    }

    private void OnTenantMessage(RedisChannel _, RedisValue value)
    {
        if (!TryDeserialize<TargetedEnvelope>(value, out var envelope) || envelope is null) return;

        foreach (var (_, conn) in _connections)
            if (conn.TenantId == envelope.TargetId)
                conn.Channel.Writer.TryWrite(envelope.Event);
    }

    private void OnBroadcastMessage(RedisChannel _, RedisValue value)
    {
        if (!TryDeserialize<SseEvent>(value, out var evt) || evt is null) return;

        foreach (var (_, conn) in _connections)
            conn.Channel.Writer.TryWrite(evt);
    }

    private static bool TryDeserialize<T>(RedisValue value, out T? result)
    {
        result = default;
        if (value.IsNullOrEmpty) return false;
        try
        {
            result = JsonSerializer.Deserialize<T>(value.ToString());
            return result is not null;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _subscriber.UnsubscribeAllAsync().ConfigureAwait(false);
        foreach (var id in _connections.Keys.ToList())
            Disconnect(id);
    }

    private sealed record TargetedEnvelope(string TargetId, SseEvent Event);
    private sealed record Connection(string UserId, string? TenantId, Channel<SseEvent> Channel);
}