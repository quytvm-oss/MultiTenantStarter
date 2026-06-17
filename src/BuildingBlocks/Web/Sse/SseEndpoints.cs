using Core.Context;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Web.Sse;

public static class SseEndpoints
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    public static IEndpointRouteBuilder MapSseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/sse/token", async (
            ICurrentUser currentUser,
            ISseTokenService tokens,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.IsAuthenticated())
                return Results.Unauthorized();

            var userId   = currentUser.GetUserId().ToString();
            var tenantId = currentUser.GetTenantId();
            var token    = await tokens.IssueAsync(userId, tenantId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new { token });
        })
        .WithName("SseToken")
        .WithSummary("Issue a short-lived SSE stream token")
        .WithTags("SSE")
        .RequireAuthorization();

        endpoints.MapGet("/api/v1/sse/stream", async (
            HttpContext context,
            [FromQuery] Guid token,
            ISseTokenService tokens,
            SseConnectionManager connectionManager,
            CancellationToken cancellationToken) =>
        {
            var principal = await tokens.ConsumeAsync(token, cancellationToken).ConfigureAwait(false);
            if (principal is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.ContentType                  = "text/event-stream";
            context.Response.Headers.CacheControl         = "no-cache";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            var (connectionId, reader) = connectionManager.Connect(principal.UserId, principal.TenantId);
            using var heartbeat = new PeriodicTimer(HeartbeatInterval);

            // Khởi tạo cả 2 task bên ngoài loop — tránh alloc mỗi iteration
            var waitTask = reader.WaitToReadAsync(cancellationToken).AsTask();
            var tickTask = heartbeat.WaitForNextTickAsync(cancellationToken).AsTask();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var completed = await Task.WhenAny(waitTask, tickTask).ConfigureAwait(false);

                    if (completed == tickTask)
                    {
                        await tickTask.ConfigureAwait(false);
                        await context.Response.WriteAsync(":heartbeat\n\n", cancellationToken).ConfigureAwait(false);
                        await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                        // Chỉ renew task đã complete
                        tickTask = heartbeat.WaitForNextTickAsync(cancellationToken).AsTask();
                        continue;
                    }

                    var hasData = await waitTask.ConfigureAwait(false);
                    if (!hasData) break;

                    // Drain hết events trong channel rồi mới flush 1 lần
                    while (reader.TryRead(out var sseEvent))
                        await WriteEventLineAsync(context.Response, sseEvent, cancellationToken).ConfigureAwait(false);

                    await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                    waitTask = reader.WaitToReadAsync(cancellationToken).AsTask();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Client disconnected — expected.
            }
            finally
            {
                connectionManager.Disconnect(connectionId);
            }
        })
        .WithName("SseStream")
        .WithSummary("Server-Sent Events stream (authenticates via ?token= issued from /sse/token)")
        .WithTags("SSE")
        .AllowAnonymous()
        .ExcludeFromDescription();

        return endpoints;
    }

    /// <summary>Writes SSE fields without flushing — caller flushes once after draining the channel.</summary>
    private static async Task WriteEventLineAsync(HttpResponse response, SseEvent sseEvent, CancellationToken ct)
    {
        if (sseEvent.Id is not null)
            await response.WriteAsync($"id: {sseEvent.Id}\n", ct).ConfigureAwait(false);

        await response.WriteAsync($"event: {sseEvent.EventType}\n", ct).ConfigureAwait(false);

        foreach (var line in sseEvent.Data.Split('\n'))
            await response.WriteAsync($"data: {line}\n", ct).ConfigureAwait(false);

        await response.WriteAsync("\n", ct).ConfigureAwait(false);
    }
}