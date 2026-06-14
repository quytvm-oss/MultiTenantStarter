using System.Text;
using System.Text.Json;

using Caching.V1.Abstractions;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Caching.V1;

public sealed partial class HybridCacheService : ICacheService
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy =  JsonNamingPolicy.CamelCase };
    
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly CachingOptions _options;

    public HybridCacheService(IMemoryCache memoryCache, 
        IDistributedCache distributedCache, 
        ILogger<HybridCacheService> logger, 
        IOptions<CachingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    /// <remarks>
    /// First checks L1 memory cache, then falls back to L2 distributed cache.
    /// If found in L2, the item is automatically populated into L1 for subsequent fast access.
    /// </remarks>
    public async Task<T?> GetItemAsync<T>(string key, CancellationToken ct = default)
    {
        key = Normalize(key);
        try
        {
            // check L1 cache first
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                LogMemoryHit(key);
                return memoryValue;
            }

            // Fall back to L2 cache (distributed)
            var bytes = await _distributedCache.GetAsync(key, ct).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0) return default;

            var distributeValue = JsonSerializer.Deserialize<T>(Utf8.GetString(bytes), JsonOpts);
            
            // Populate L1 cache from L2
            if (distributeValue is not null)
            {
                var expiration = GetMemoryCacheExpiration();
                _memoryCache.Set(key, distributeValue, expiration);
                LogPopulatedFromDistributed(key);
            }
            
            return distributeValue;
        }
        // Graceful degradation: cache failures must not crash the caller — return default and log
        catch (Exception e) when (e is not OperationCanceledException)
        {
           _logger.LogWarning(e, "Cache get failed for key (length={KeyLength})", key.Length);
           return default;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Writes to both L1 memory cache and L2 distributed cache simultaneously.
    /// </remarks>
    public async Task SetItemAsync<T>(string key, T value, TimeSpan? sliding = default, CancellationToken ct = default)
    {
        key = Normalize(key);
        try
        {
            var bytes = Utf8.GetBytes(JsonSerializer.Serialize(value, JsonOpts));
            await _distributedCache.SetAsync(key, bytes, BuildDistributedEntryOptions(sliding), ct)
                .ConfigureAwait(false);
            
            // Also set in memory cache
            var expiration = GetMemoryCacheExpiration();
            _memoryCache.Set(key, value, expiration);
            
            LogCachedBoth(key);
        }
        // Graceful degradation: cache failures must not crash the caller — return default and log
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Cache set failed for key (length={KeyLength})", key.Length);
        }
    }

    public async Task SetItemAsync<T>(string key, T value, IReadOnlyList<string> tags, TimeSpan? sliding = default,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tags);
        await SetItemAsync(key, value, sliding, ct);

        // Store the key in each tag's set so we can invalidate by tag later
        foreach (var tag in tags)
        {
            var tagKey = NormalizeTagKey(tag);
            var existing = await GetItemAsync<HashSet<string>>(tagKey, ct).ConfigureAwait(false) ?? [];
            existing.Add(Normalize(key));
            await SetItemAsync(tagKey, existing, sliding, ct).ConfigureAwait(false);
        }
    }
    
    /// <inheritdoc />
    /// <remarks>
    /// Removes from both L1 memory cache and L2 distributed cache.
    /// </remarks>
    public async Task RemoveItemAsync(string key, CancellationToken ct = default)
    {
        key = Normalize(key);
        try
        {
            // Remove from both caches
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key, ct).ConfigureAwait(false);
            LogRemoveBoth(key);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Cache remove failed for {key}", key);
        }
    }

    public async Task RefreshItemAsync(string key, CancellationToken ct = default)
    {
        key = Normalize(key);
        try
        {
            await _distributedCache.RefreshAsync(key, ct).ConfigureAwait(false);
            LogRefreshed(key);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Cache refresh failed for {key}", key);
        }
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
        var tagKey = NormalizeTagKey(tag);
        var keys = await GetItemAsync<HashSet<string>>(tagKey, ct).ConfigureAwait(false);
        if (keys is null || keys.Count == 0) return;

        foreach (var key in keys)
        {
            try
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key, ct).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "Cache remove by tag failed for {key}", key);
            }
        }
        
        _memoryCache.Remove(tagKey);
        await _distributedCache.RemoveAsync(tagKey, ct).ConfigureAwait(false);
    }

    private string NormalizeTagKey(string tag) => Normalize($"__tag:{tag}");

    private DistributedCacheEntryOptions BuildDistributedEntryOptions(TimeSpan? sliding)
    {
        var o = new DistributedCacheEntryOptions();

        if (sliding.HasValue)
            o.SetSlidingExpiration(sliding.Value);
        else if (_options.DefaultSlidingExpiration.HasValue)
            o.SetSlidingExpiration(_options.DefaultSlidingExpiration.Value);
        
        if (_options.DefaultAbsoluteExpiration.HasValue)
            o.SetAbsoluteExpiration(_options.DefaultAbsoluteExpiration.Value);
        
        return o;
    }
    
    private MemoryCacheEntryOptions GetMemoryCacheExpiration()
    {
        var options = new MemoryCacheEntryOptions();
        
        // Use shorter expiration for memory cache (faster refreshed from distributed cache)
        var slidingExpiration = _options.DefaultSlidingExpiration ?? TimeSpan.FromMinutes(1);
        options.SetSlidingExpiration(TimeSpan.FromSeconds(slidingExpiration.TotalSeconds * 0.8)); // 80% of distributed cache expiration
        
        return options;
    }

    private string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        var prefix = _options.KeyPrefix ?? string.Empty;
        if (prefix.Length == 0)
            return key;
        
        return key.StartsWith(prefix, StringComparison.Ordinal)
            ? key.Substring(prefix.Length)
            : key;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit in memory for {key}")]
    private partial void LogMemoryHit(string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Populated memory cache from distributed cache for {Key}")]
    private partial void LogPopulatedFromDistributed(string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached both in memory and distributed cache for {Key}")]
    private partial void LogCachedBoth(string key);
    
    [LoggerMessage(Level = LogLevel.Debug, Message = "Removed both from memory and distributed cache for {Key}")]
    private partial void LogRemoveBoth(string key);
    
    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshed {Key}")]
    private partial void LogRefreshed(string key);
}