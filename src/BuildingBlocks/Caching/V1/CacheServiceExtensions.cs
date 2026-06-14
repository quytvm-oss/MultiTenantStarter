using Caching.V1.Abstractions;

namespace Caching.V1;

public static class CacheServiceExtensions
{
    /// <summary>
    /// Retrieves an item from the cache associated with the specified key, or computes and stores it in the cache if the item does not already exist.
    /// </summary>
    /// <typeparam name="T">The type of the item to retrieve or compute and store in the cache.</typeparam>
    /// <param name="cache">The instance of <see cref="ICacheService"/> used to access the cache.</param>
    /// <param name="key">The unique key identifying the cached item.</param>
    /// <param name="task">A function that asynchronously computes the value to store in the cache if the key is not found.</param>
    /// <param name="slidingExpiration">An optional sliding expiration time for the cached item. If provided, the item expiration will be reset on access.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous operation, containing the cached or computed item of type T.</returns>
    public static async Task<T?> GetOrSetAsync<T>(this ICacheService cache, string key, Func<Task<T>> task,
        TimeSpan? slidingExpiration = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        
        T? value = await cache.GetItemAsync<T>(key, ct);
        
        if (value is not null)
            return value;
        
        ArgumentNullException.ThrowIfNull(task);
        value = await task();

        if (value is not null)
        {
            await cache.SetItemAsync(key, value, slidingExpiration, ct);
        }

        return value;
    }
}