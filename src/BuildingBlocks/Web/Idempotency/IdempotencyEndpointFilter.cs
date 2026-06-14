using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Caching.V2;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Idempotency;

// public sealed class IdempotencyEndpointFilter : IEndpointFilter
// {
//     private static readonly JsonSerializerOptions JsonOpts =
//         new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
//  
//     private readonly IDistributedCache _distributedCache;
//     private readonly HybridCache _hybridCache;
//     private readonly IDistributedLock _distributedLock;
//     private readonly ILogger<IdempotencyEndpointFilter> _logger;
//     private readonly IdempotencyOptions _options;
//  
//     public IdempotencyEndpointFilter(
//         IDistributedCache distributedCache,
//         HybridCache hybridCache,
//         IDistributedLock distributedLock,
//         ILogger<IdempotencyEndpointFilter> logger,
//         IOptions<IdempotencyOptions> options)
//     {
//         _distributedCache = distributedCache;
//         _hybridCache = hybridCache;
//         _distributedLock = distributedLock;
//         _logger = logger;
//         _options = options.Value;
//     }
//  
//     public async ValueTask<object?> InvokeAsync(
//         EndpointFilterInvocationContext context,
//         EndpointFilterDelegate next)
//     {
//         ArgumentNullException.ThrowIfNull(context);
//         ArgumentNullException.ThrowIfNull(next);
//  
//         var httpContext = context.HttpContext;
//         var idempotencyKey = httpContext.Request.Headers[_options.HeaderName].ToString();
//  
//         // No header = pass through (idempotency is opt-in per request)
//         if (string.IsNullOrWhiteSpace(idempotencyKey))
//         {
//             return await next(context).ConfigureAwait(false);
//         }
//  
//         if (idempotencyKey.Length > _options.MaxKeyLength)
//         {
//             return TypedResults.BadRequest(
//                 $"Idempotency key exceeds maximum length of {_options.MaxKeyLength}.");
//         }
//  
//         // Include tenant context in cache key for isolation
//         var tenantId = httpContext.User.FindFirst("tenant")?.Value ?? "global";
//         var cacheKey = CacheKeys.IdempotencyEntry(tenantId, idempotencyKey);
//         var lockKey = CacheKeys.IdempotencyLock(tenantId, idempotencyKey);
//         var tags = new[] { CacheKeys.Tags.Idempotency, CacheKeys.Tags.Tenant(tenantId) };
//  
//         // First probe (no lock): fast path for replays — most requests are first-calls.
//         var replay = await TryGetCachedResponseAsync(cacheKey, httpContext).ConfigureAwait(false);
//         if (replay is not null)
//         {
//             return replay;
//         }
//  
//         // Acquire distributed lock to prevent concurrent duplicate requests from both
//         // executing the handler. Lock timeout = request deadline capped to a reasonable max.
//         await using var lockHandle = await _distributedLock
//             .TryAcquireAsync(lockKey, timeout: _options.LockTimeout, httpContext.RequestAborted)
//             .ConfigureAwait(false);
//  
//         if (lockHandle is null)
//         {
//             // Could not acquire lock within timeout — another instance is processing this key.
//             // Return 409 so the client knows to retry after the first request completes.
//             _logger.LogWarning(
//                 "Idempotency lock contention for key {KeyHash}", HashKey(idempotencyKey));
//             return TypedResults.Conflict(
//                 "A request with this idempotency key is already being processed. Retry shortly.");
//         }
//  
//         // Re-check cache after acquiring lock (another instance may have just written it).
//         replay = await TryGetCachedResponseAsync(cacheKey, httpContext).ConfigureAwait(false);
//         if (replay is not null)
//         {
//             return replay;
//         }
//  
//         // Execute the handler and capture the raw HTTP response body so we store exactly
//         // what the client will receive — including responses from IResult.ExecuteAsync.
//         var originalBody = httpContext.Response.Body;
//         using var captureStream = new MemoryStream();
//         httpContext.Response.Body = captureStream;
//  
//         object? result;
//         try
//         {
//             result = await next(context).ConfigureAwait(false);
//  
//             // IResult types (TypedResults.*) write directly to Response.Body via ExecuteAsync.
//             // Serializing the IResult object itself would produce meaningless JSON.
//             // We therefore capture the already-written body bytes as the canonical response.
//             if (result is IResult iResult)
//             {
//                 await iResult.ExecuteAsync(httpContext).ConfigureAwait(false);
//                 result = null; // Signal that response is already written.
//             }
//         }
//         finally
//         {
//             // Flush capture stream back to the original body regardless of outcome.
//             captureStream.Seek(0, SeekOrigin.Begin);
//             await captureStream.CopyToAsync(originalBody, httpContext.RequestAborted)
//                 .ConfigureAwait(false);
//             httpContext.Response.Body = originalBody;
//         }
//  
//         // Only cache successful (2xx) responses — don't replay 4xx/5xx as idempotent.
//         var statusCode = httpContext.Response.StatusCode;
//         if (statusCode is < 200 or >= 300)
//         {
//             return result;
//         }
//  
//         // Build the cache entry from captured bytes.
//         byte[] body;
//         if (result is not null)
//         {
//             // Object result (non-IResult): serialize now (IResult already wrote to capture stream).
//             body = JsonSerializer.SerializeToUtf8Bytes(result, JsonOpts);
//         }
//         else
//         {
//             body = captureStream.ToArray();
//         }
//  
//         await TryCacheResponseAsync(
//             cacheKey,
//             tags,
//             statusCode,
//             httpContext.Response.ContentType,
//             body,
//             httpContext.RequestAborted)
//             .ConfigureAwait(false);
//  
//         return result;
//     }
//  
//     private async Task<object?> TryGetCachedResponseAsync(
//         string cacheKey,
//         HttpContext httpContext)
//     {
//         // Probe-only read via IDistributedCache (real GetAsync, null on miss — unlike HybridCache's
//         // factory). Bypasses L1: replays are rare vs first-calls, so L1 warmth has little value.
//         var cachedBytes = await _distributedCache
//             .GetAsync(cacheKey, httpContext.RequestAborted)
//             .ConfigureAwait(false);
//  
//         if (cachedBytes is not { Length: > 0 })
//         {
//             return null;
//         }
//  
//         var cached = JsonSerializer.Deserialize<CachedIdempotentResponse>(cachedBytes, JsonOpts);
//         if (cached is null)
//         {
//             return null;
//         }
//  
//         if (_logger.IsEnabled(LogLevel.Debug))
//         {
//             _logger.LogDebug(
//                 "Idempotent replay for tenant {TenantId}, key {KeyHash}",
//                 httpContext.User.FindFirst("tenant")?.Value ?? "global",
//                 HashKey(httpContext.Request.Headers[_options.HeaderName].ToString()));
//         }
//  
//         httpContext.Response.Headers["Idempotency-Replayed"] = "true";
//         httpContext.Response.StatusCode = cached.StatusCode;
//  
//         if (cached.ContentType is not null)
//         {
//             httpContext.Response.ContentType = cached.ContentType;
//         }
//  
//         if (cached.Body.Length > 0)
//         {
//             await httpContext.Response.Body
//                 .WriteAsync(cached.Body, httpContext.RequestAborted)
//                 .ConfigureAwait(false);
//         }
//  
//         return null; // Response already written
//     }
//  
//     private async Task TryCacheResponseAsync(
//         string cacheKey,
//         string[] tags,
//         int statusCode,
//         string? contentType,
//         byte[] body,
//         CancellationToken cancellationToken)
//     {
//         try
//         {
//             var responseToCache = new CachedIdempotentResponse
//             {
//                 StatusCode = statusCode,
//                 ContentType = contentType,
//                 Body = body
//             };
//  
//             var setOptions = new HybridCacheEntryOptions
//             {
//                 Expiration = _options.DefaultTtl,
//                 LocalCacheExpiration = _options.DefaultTtl < TimeSpan.FromMinutes(2)
//                     ? _options.DefaultTtl
//                     : TimeSpan.FromMinutes(2),
//             };
//  
//             // Write via HybridCache so tag invalidation path works for purges.
//             await _hybridCache
//                 .SetAsync(cacheKey, responseToCache, setOptions, tags, cancellationToken)
//                 .ConfigureAwait(false);
//         }
//         // Best-effort caching: idempotency replay is a convenience, not a correctness requirement.
//         catch (Exception ex) when (ex is not OperationCanceledException)
//         {
//             _logger.LogWarning(ex, "Failed to cache idempotent response for key {CacheKey}", cacheKey);
//         }
//     }
//  
//     private static string HashKey(string key)
//     {
//         var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
//         return Convert.ToHexString(hash.AsSpan(0, 8));
//     }
// }

public sealed class IdempotencyEndpointFilter : IEndpointFilter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy =  JsonNamingPolicy.CamelCase };
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        
        var httpContext = context.HttpContext;
        var options = httpContext.RequestServices.GetRequiredService<IOptions<IdempotencyOptions>>().Value;
        var idempotencyKey = httpContext.Request.Headers[options.HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return await next(context).ConfigureAwait(false);
        }
        
        if (idempotencyKey.Length > options.MaxKeyLength)
            return TypedResults.BadRequest($"Idempotency key exceeds maximum length of {options.MaxKeyLength}.");
        
        
        var distributedCache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var hybridCache = httpContext.RequestServices.GetRequiredService<HybridCache>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotencyEndpointFilter>>();
        
        // Include tenant context in cache key for isolation
        var tenantId = httpContext.User.FindFirst("tenant")?.Value ?? "global";
        var cacheKey = CacheKeys.IdempotencyEntry(tenantId, idempotencyKey);
        var tags = new[] { CacheKeys.Tags.Idempotency, CacheKeys.Tags.Tenant(tenantId) };
        
        // Probe-only read via IDistributedCache (real GetAsync, null on miss — unlike HybridCache's
        // factory). Bypasses L1: replays are rare vs first-calls, so L1 warmth has little value.
        var cachedBytes = await distributedCache.GetAsync(cacheKey, httpContext.RequestAborted).ConfigureAwait(false);
        if (cachedBytes is not null && cachedBytes.Length > 0)
        {
            var cached = JsonSerializer.Deserialize<CachedIdempotentResponse>(cachedBytes, JsonOpts);
            if (cached is not null)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Idempotent replay for key {KeyHash}", HashKey(idempotencyKey));
                }
                httpContext.Response.Headers["Idempotency-Replayed"] = "true";
                httpContext.Response.StatusCode = cached.StatusCode;
                if (cached.ContentType is not null)
                {
                    httpContext.Response.ContentType = cached.ContentType;
                }

                if (cached.Body.Length > 0)
                {
                    await httpContext.Response.Body.WriteAsync(cached.Body, httpContext.RequestAborted).ConfigureAwait(false);
                }

                return null; // Response already written
            }
        }
        
        // Execute the handler
        var result = await next(context).ConfigureAwait(false);
        
        // Cache the response through HybridCache so the tag invalidation path works for purges.
        try
        {
            var body = result is not null ? JsonSerializer.SerializeToUtf8Bytes(result, JsonOpts) : [];
            var responseToCache = new CachedIdempotentResponse
            {
                StatusCode = httpContext.Response.StatusCode is > 0 and < 600 ? httpContext.Response.StatusCode : 200,
                ContentType = "application/json",
                Body = body
            };

            var setOptions = new HybridCacheEntryOptions
            {
                Expiration = options.DefaultTtl,
                LocalCacheExpiration = options.DefaultTtl < TimeSpan.FromMinutes(2) ? options.DefaultTtl : TimeSpan.FromMinutes(2),
            };
            await hybridCache.SetAsync(cacheKey, responseToCache, setOptions, tags, httpContext.RequestAborted).ConfigureAwait(false);
        }
        // Best-effort caching: idempotency replay is a convenience, not a correctness requirement
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to cache idempotent response for key {KeyHash}", HashKey(idempotencyKey));
        }

        return result;
    }
    
    private static string HashKey(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}