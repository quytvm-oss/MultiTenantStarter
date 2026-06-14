using System.Collections.Frozen;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Web.Realtime;

/// <summary>
/// Redis-backed presence tracker. Replaces the in-memory <c>PresenceTracker</c> for multi-instance
/// deployments. Each user's active connection count is stored as a Redis string key
/// (<c>presence:{userId}</c>) and incremented/decremented atomically via INCR/DECR. A safety TTL
/// of 24 h is refreshed on every connect to guard against leaked keys (e.g. instance crash without
/// graceful disconnect).
///
/// <para>
/// Transition semantics mirror the original in-memory implementation:
/// <list type="bullet">
///   <item><c>ConnectAsync</c> returns <c>true</c> only on the 0→1 transition (offline→online).</item>
///   <item><c>DisconnectAsync</c> returns <c>true</c> only on the N→0 transition (online→offline).</item>
/// </list>
/// These booleans gate the <c>PresenceChanged</c> SignalR broadcasts in <see cref="AppHub"/> so
/// the hub only fans out when the user's effective status actually changes, not on every tab open/close.
/// </para>
///
/// <para>
/// <b>Bulk presence</b> (<see cref="GetStatusAsync"/>) uses a single Redis pipeline (MGET) so the
/// presence endpoint stays O(1) round-trips regardless of how many user IDs are queried. This is
/// important because the endpoint is polled frequently by clients on initial load.
/// </para>
/// </summary>
public sealed class RedisPresenceTracker : IPresenceTracker
{
    /// <summary>
    /// Safety TTL refreshed on every connect. Guards against keys leaked by instance crashes
    /// that skip <see cref="AppHub.OnDisconnectedAsync"/>. 24 h is generous enough that a user
    /// reconnecting after a long sleep won't lose their presence, but short enough that stale
    /// keys don't accumulate indefinitely.
    /// </summary>
    private static readonly TimeSpan KeyTtl = TimeSpan.FromHours(24);
 
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPresenceTracker> _logger;
 
    public RedisPresenceTracker(IConnectionMultiplexer redis, ILogger<RedisPresenceTracker> logger)
    {
        _redis = redis;
        _logger = logger;
    }
 
    /// <inheritdoc />
    /// <remarks>
    /// Atomically increments the connection counter. Returns <c>true</c> when the counter
    /// transitions from 0 (or missing) to 1 — i.e. this is the user's first active connection.
    /// </remarks>
    public async Task<bool> ConnectAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        const string lua = """
                           local v = redis.call('INCR', KEYS[1])
                           redis.call('EXPIRE', KEYS[1], ARGV[1])
                           return v
                           """;

        var db = _redis.GetDatabase();
        var key = KeyFor(userId);

        var newCount = (long)await db.ScriptEvaluateAsync(
            lua,
            keys: [key],
            values: [(long)KeyTtl.TotalSeconds]).ConfigureAwait(false);

        var transitioned = newCount == 1;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Presence connect: user={UserId} count={Count} transitioned={Transitioned}",
                userId, newCount, transitioned);
        }

        return transitioned;
    }
 
    /// <inheritdoc />
    /// <remarks>
    /// Atomically decrements the connection counter, clamping to zero. Returns <c>true</c>
    /// when the user goes fully offline (all tabs / connections closed).
    ///
    /// <para>
    /// The decrement uses a Lua script to atomically read-decrement-clamp in one round-trip,
    /// avoiding the read-then-write TOCTOU race present in the original in-memory implementation.
    /// </para>
    /// </remarks>
    public async Task<bool> DisconnectAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
 
        // Lua script: decrement but never go below 0. Returns the value BEFORE the decrement
        // so we can detect the N→0 transition without a separate read.
        // KEYS[1] = presence key
        const string lua = """
            local v = redis.call('GET', KEYS[1])
            if not v or tonumber(v) <= 0 then
                redis.call('DEL', KEYS[1])
                return 0
            end
            local newVal = tonumber(v) - 1
            if newVal <= 0 then
                redis.call('DEL', KEYS[1])
                return 0
            end
            redis.call('SET', KEYS[1], newVal, 'KEEPTTL')
            return newVal
            """;
 
        var db = _redis.GetDatabase();
        var key = KeyFor(userId);
 
        var result = (long)await db.ScriptEvaluateAsync(lua, keys: [key]).ConfigureAwait(false);
        var transitioned = result == 0;
 
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Presence disconnect: user={UserId} remainingCount={Count} transitioned={Transitioned}",
                userId, result, transitioned);
        }
 
        return transitioned;
    }
 
    /// <inheritdoc />
    public async Task<bool> IsOnlineAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;
 
        var val = await _redis.GetDatabase().StringGetAsync(KeyFor(userId)).ConfigureAwait(false);
        return val.TryParse(out long count) && count > 0;
    }
 
    /// <inheritdoc />
    /// <remarks>
    /// Uses a single Redis pipeline (MGET) — O(1) round-trips regardless of how many IDs are
    /// queried. This matters because the presence endpoint is polled frequently by clients.
    /// </remarks>
    public async Task<IReadOnlyDictionary<string, bool>> GetStatusAsync(
        IEnumerable<string> userIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
 
        // Deduplicate and filter blanks before hitting Redis.
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
 
        if (ids.Count == 0)
            return FrozenDictionary<string, bool>.Empty;
 
        var db = _redis.GetDatabase();
        var keys = ids.Select(id => (RedisKey)KeyFor(id)).ToArray();
 
        // Single round-trip via MGET.
        var values = await db.StringGetAsync(keys).ConfigureAwait(false);
 
        var result = new Dictionary<string, bool>(ids.Count, StringComparer.Ordinal);
        for (var i = 0; i < ids.Count; i++)
        {
            var online = values[i].TryParse(out long count) && count > 0;
            result[ids[i]] = online;
        }
 
        return result;
    }
 
    private static string KeyFor(string userId) => $"presence:{userId}";
}
 