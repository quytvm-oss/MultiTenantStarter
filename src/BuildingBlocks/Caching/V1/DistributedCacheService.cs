using System.Text;
using System.Text.Json;

using Caching.V1.Abstractions;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Caching.V1;

public sealed partial class DistributedCacheService : ICacheService
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy =  JsonNamingPolicy.CamelCase };
    
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly CachingOptions _options;

    public DistributedCacheService(
        ILogger<DistributedCacheService> logger, 
        IDistributedCache cache, 
        IOptions<CachingOptions> options)
    {
        _logger = logger;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<T?> GetItemAsync<T>(string key, CancellationToken ct = default)
    {
        key = Normalizes(key);
        try
        {
            var bytes = await _cache.GetAsync(key, ct).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0) return default;
            return JsonSerializer.Deserialize<T>(Utf8.GetString(bytes), JsonOpts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache get failed for key (length={KeyLength})", key.Length);
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value, TimeSpan? sliding = default, CancellationToken ct = default)
    {
        key = Normalizes(key);
        try
        {
            var bytes = Utf8.GetBytes(JsonSerializer.Serialize(value, JsonOpts));
            await _cache.SetAsync(key, bytes, BuildCacheEntryOptions(sliding), ct).ConfigureAwait(false);
            LogCached(key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache set failed for key (length={KeyLength})", key.Length);
        }
    }

    public async Task SetItemAsync<T>(string key, T value, IReadOnlyList<string> tags, TimeSpan? sliding = default,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tags);
        await SetItemAsync(key, value, sliding, ct);

        foreach (var tag in tags)
        {
            var tagKey = NormalizeTagKey(tag);
            var existing = await GetItemAsync<HashSet<string>>(tagKey, ct).ConfigureAwait(false) ?? [];
            existing.Add(Normalizes(key));
            await SetItemAsync(tagKey, existing, sliding, ct).ConfigureAwait(false);
        }
    }

    public async Task RemoveItemAsync(string key, CancellationToken ct = default)
    {
        key = Normalizes(key);
        try
        {
            await _cache.RemoveAsync(key, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache remove failed for {key}", key);
        }
    }

    public async Task RefreshItemAsync(string key, CancellationToken ct = default)
    {
        key = Normalizes(key);
        try
        {
            await _cache.RefreshAsync(key, ct).ConfigureAwait(false);
            LogRefreshed(key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache refresh failed for {key}", key);
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
                await _cache.RemoveAsync(key, ct).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "Cache remove by tag failed for {key}", key);
            }
        }
        
        await RemoveItemAsync(tagKey, ct).ConfigureAwait(false);
    }
    
    private string NormalizeTagKey(string tag) => Normalizes($"__tag:{tag}");

    private DistributedCacheEntryOptions BuildCacheEntryOptions(TimeSpan? sliding)
    {
        var o =  new DistributedCacheEntryOptions();
        if (sliding.HasValue)
            o.SetSlidingExpiration(sliding.Value);
        else if (_options.DefaultSlidingExpiration.HasValue)
            o.SetAbsoluteExpiration(_options.DefaultSlidingExpiration.Value);
        
        if (_options.DefaultAbsoluteExpiration.HasValue)
            o.SetAbsoluteExpiration(_options.DefaultAbsoluteExpiration.Value);
        
        return o;
    }

    private string Normalizes(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        var prefix = _options.KeyPrefix ?? string.Empty;
        if (prefix.Length == 0)
            return key;
        
        return key.StartsWith(prefix, StringComparison.Ordinal)
            ? key : prefix + key;
    }
    
    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached {Key}")]
    private partial void LogCached(string key);
    
    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshed {Key}")]
    private partial void LogRefreshed(string key);
}