namespace Web.Realtime;

/// <summary>
/// Tracks per-user connection counts across all active SignalR connections.
/// Implementations must be thread-safe and support multi-instance deployments.
///
/// <para>
/// The tracker counts raw connections (tabs, devices) rather than exposing a simple bool.
/// This lets the hub fire <c>PresenceChanged</c> only on true status transitions:
/// <list type="bullet">
///   <item>offline→online: user's first connection (count 0→1)</item>
///   <item>online→offline: user's last connection closed (count N→0)</item>
/// </list>
/// All intermediate tab opens/closes are silent.
/// </para>
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    /// Records a new connection for <paramref name="userId"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this is the user's first connection (offline→online transition);
    /// <c>false</c> if the user was already online.
    /// </returns>
    Task<bool> ConnectAsync(string userId, CancellationToken ct = default);
 
    /// <summary>
    /// Records a closed connection for <paramref name="userId"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this was the user's last connection (online→offline transition);
    /// <c>false</c> if the user still has other active connections.
    /// </returns>
    Task<bool> DisconnectAsync(string userId, CancellationToken ct = default);
 
    /// <summary>
    /// Returns whether <paramref name="userId"/> has at least one active connection.
    /// </summary>
    Task<bool> IsOnlineAsync(string userId, CancellationToken ct = default);
 
    /// <summary>
    /// Returns the online status for multiple users in a single round-trip.
    /// Missing users are returned as <c>false</c>.
    /// </summary>
    Task<IReadOnlyDictionary<string, bool>> GetStatusAsync(IEnumerable<string> userIds, CancellationToken ct = default);
}