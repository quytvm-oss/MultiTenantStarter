using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Web.Realtime;

[Authorize]
public class AppHub : Hub
{
    private static readonly TimeSpan TypingThrottle = TimeSpan.FromSeconds(3);
 
    private readonly IChannelMembershipChecker _membership;
    private readonly IDistributedCache _cache;
    private readonly IUserChannelLookup _channels;
    private readonly IPresenceTracker _presence;
    private readonly ILogger<AppHub> _logger;
    
    
    public AppHub(
        IChannelMembershipChecker membership,
        IDistributedCache cache,
        IUserChannelLookup channels,
        IPresenceTracker presence,
        ILogger<AppHub> logger)
    {
        _membership = membership;
        _cache = cache;
        _channels = channels;
        _presence = presence;
        _logger = logger;
    }
 
    /// <summary>
    /// Reads the authenticated user id off the connection's principal. Cannot use
    /// <c>ICurrentUser</c> here because it resolves through <c>IHttpContextAccessor</c> — the
    /// originating negotiate <c>HttpContext</c> is not pinned to subsequent hub method invocations,
    /// so any indirection through it returns nulls.
    /// </summary>
    private string? GetUserId()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true) return null;
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("uid");
    }
 
    private string? GetTenantId()
    {
        var user = Context.User;
        if (user is null) return null;
        return user.FindFirstValue("tenant")
            ?? user.FindFirstValue("tid")
            ?? user.FindFirstValue("tenantId");
    }
 
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId) || userId == Guid.Empty.ToString())
            {
                Context.Abort();
                return;
            }
 
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}", Context.ConnectionAborted)
                .ConfigureAwait(false);
 
            var tenantId = GetTenantId();
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}", Context.ConnectionAborted)
                    .ConfigureAwait(false);
            }
 
            var channelIds = await _channels
                .ListMyChannelIdsAsync(userId, Context.ConnectionAborted)
                .ConfigureAwait(false);
 
            foreach (var channelId in channelIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}", Context.ConnectionAborted)
                    .ConfigureAwait(false);
            }
 
            AppHubLog.Connected(_logger, Context.ConnectionId, userId, channelIds.Count);
 
            // ConnectAsync returns true only on 0→1 transition — avoids redundant broadcasts
            // when the user already has other tabs open.
            if (await _presence.ConnectAsync(userId, Context.ConnectionAborted).ConfigureAwait(false))
            {
                var target = string.IsNullOrEmpty(tenantId)
                    ? Clients.All
                    : Clients.Group($"tenant:{tenantId}");
 
                await target.SendAsync(
                        "PresenceChanged",
                        new { userId, online = true },
                        Context.ConnectionAborted)
                    .ConfigureAwait(false);
            }
 
            await base.OnConnectedAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
        {
            // Client disconnected mid-connect — expected, swallow.
        }
    }
 
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            // DisconnectAsync returns true only on N→0 transition.
            if (await _presence.DisconnectAsync(userId).ConfigureAwait(false))
            {
                var tenantId = GetTenantId();
                var target = string.IsNullOrEmpty(tenantId)
                    ? Clients.All
                    : Clients.Group($"tenant:{tenantId}");
 
                await target.SendAsync(
                        "PresenceChanged",
                        new { userId, online = false })
                    .ConfigureAwait(false);
            }
        }
 
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
 
    /// <summary>
    /// Client invokes <c>Typing(channelId)</c> while composing. Throttled to once per 3s per
    /// (channel, user) via the distributed cache so chatty UIs don't flood the wire.
    /// </summary>
    public async Task Typing(Guid channelId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;
 
        if (!await _membership.IsMemberAsync(channelId, userId, Context.ConnectionAborted).ConfigureAwait(false))
            return;
 
        var key = $"typing:{channelId}:{userId}";
        var existing = await _cache.GetStringAsync(key, Context.ConnectionAborted).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(existing)) return;
 
        await _cache.SetStringAsync(
                key,
                "1",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TypingThrottle },
                Context.ConnectionAborted)
            .ConfigureAwait(false);
 
        await Clients.OthersInGroup($"channel:{channelId}")
            .SendAsync("ChatTypingStarted", new { channelId, userId }, Context.ConnectionAborted)
            .ConfigureAwait(false);
    }
 
    /// <summary>
    /// Client invokes <c>JoinChannel(channelId)</c> when it opens a conversation.
    /// Handles channels created after the socket was opened. Idempotent.
    /// </summary>
    public async Task JoinChannel(Guid channelId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;
 
        if (!await _membership.IsMemberAsync(channelId, userId, Context.ConnectionAborted).ConfigureAwait(false))
            return;
 
        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}", Context.ConnectionAborted)
            .ConfigureAwait(false);
    }
}

internal static partial class AppHubLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "AppHub connection {ConnectionId} for user {UserId} pre-joined {ChannelCount} channel groups")]
    public static partial void Connected(ILogger logger, string connectionId, string userId, int channelCount);
}