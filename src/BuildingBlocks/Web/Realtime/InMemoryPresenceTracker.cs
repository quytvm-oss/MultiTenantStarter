using System.Collections.Concurrent;

namespace Web.Realtime;

/// <summary>
/// In-memory presence tracker for single-host deployments (dev, tests).
/// Implements the same <see cref="IPresenceTracker"/> interface as <see cref="RedisPresenceTracker"/>
/// so the registration swap in <see cref="HeroRealtimeExtensions"/> is the only change needed.
///
/// <para>
/// Not suitable for multi-instance deployments — use <see cref="RedisPresenceTracker"/> instead.
/// </para>
/// </summary>
internal sealed class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);
 
    public Task<bool> ConnectAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
 
        var transitioned = false;
        _counts.AddOrUpdate(
            userId,
            _ => { transitioned = true; return 1; },
            (_, prev) =>
            {
                if (prev == 0) transitioned = true;
                return prev + 1;
            });
 
        return Task.FromResult(transitioned);
    }
 
    public Task<bool> DisconnectAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
 
        // Spin-loop to handle concurrent disconnects without a lock.
        while (true)
        {
            if (!_counts.TryGetValue(userId, out var current))
                return Task.FromResult(false);
 
            if (current <= 1)
            {
                if (!_counts.TryUpdate(userId, 0, current)) continue; // retry
                _counts.TryRemove(new KeyValuePair<string, int>(userId, 0));
                return Task.FromResult(true);
            }
 
            if (_counts.TryUpdate(userId, current - 1, current))
                return Task.FromResult(false);
            // else retry
        }
    }
 
    public Task<bool> IsOnlineAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.FromResult(false);
        return Task.FromResult(_counts.TryGetValue(userId, out var count) && count > 0);
    }
 
    public Task<IReadOnlyDictionary<string, bool>> GetStatusAsync(
        IEnumerable<string> userIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
 
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var id in userIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            result[id] = _counts.TryGetValue(id, out var count) && count > 0;
        }
 
        return Task.FromResult<IReadOnlyDictionary<string, bool>>(result);
    }
}
 