namespace Caching.V1.Abstractions;

public interface ICacheService
{
    /// <summary>
    /// Retrieves an item from the cache asynchronously based on the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the item to retrieve from the cache.</typeparam>
    /// <param name="key">The unique key identifying the cached item.</param>
    /// <param name="ct">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation that, when completed, contains the cached item as an instance of type T, or null if the item is not found.</returns>
    Task<T?> GetItemAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Stores an item in the cache asynchronously with the specified key and optional sliding expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the item to store in the cache.</typeparam>
    /// <param name="key">The unique key identifying the item to be stored.</param>
    /// <param name="value">The value of the item to be stored in the cache.</param>
    /// <param name="sliding">An optional sliding expiration time for the cached item. If provided, the item expiration will be reset on access.</param>
    /// <param name="ct">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetItemAsync<T>(string key, T value, TimeSpan? sliding = default, CancellationToken ct = default);

    /// <summary>
    /// Stores an item in the cache asynchronously with the specified key, value, and optional sliding expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the item to store in the cache.</typeparam>
    /// <param name="key">The unique key to associate with the item in the cache.</param>
    /// <param name="value">The value of the item to store in the cache.</param>
    /// <param name="sliding">The optional sliding expiration time, after which the cache entry will expire if it hasn't been accessed.</param>
    /// <param name="ct">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetItemAsync<T>(string key, T value, IReadOnlyList<string> tags, TimeSpan? sliding = default,
        CancellationToken ct = default);

    /// <summary>
    /// Removes an item from the cache asynchronously based on the specified key.
    /// </summary>
    /// <param name="key">The unique key identifying the cached item to remove.</param>
    /// <param name="ct">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation of removing the item from the cache.</returns>
    Task RemoveItemAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Refreshes an item in the cache asynchronously, ensuring it is up-to-date based on the specified key.
    /// </summary>
    /// <param name="key">The unique key identifying the cached item to refresh.</param>
    /// <param name="ct">A CancellationToken to observe while waiting for the operation to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshItemAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Removes items from the cache asynchronously that are associated with the specified tag.
    /// </summary>
    /// <param name="tag">The tag associated with the cached items to be removed.</param>
    /// <param name="ct">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation to remove the items associated with the specified tag.</returns>
    Task RemoveByTagAsync(string tag, CancellationToken ct = default);
}