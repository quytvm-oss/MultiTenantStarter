namespace Caching.V2;

public class CachingOptions
{
    public string Redis { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS for Redis connection. If null, uses the connection string default.
    /// Aspire 13.x defaults Redis to TLS on the primary port; set to <c>false</c> when wiring via
    /// the plain-TCP secondary endpoint (see <c>AppHost.cs</c>).
    /// </summary>
    public bool? EnableSsl { get; set; }

    /// <summary>
    /// Total lifetime of a cache entry across both L1 (in-process) and L2 (Redis).
    /// Applied as the default when the caller doesn't pass <c>HybridCacheEntryOptions</c>.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Lifetime of the L1 in-process copy. Kept short to bound cross-node staleness after
    /// a <c>RemoveAsync</c>/<c>RemoveByTagAsync</c> on a peer node, since HybridCache has no
    /// built-in L1 backplane. See <c>docs/caching.md</c> for the tradeoff.
    /// </summary>
    public TimeSpan DefaultLocalCacheExpiration { get; set; } = TimeSpan.FromMinutes(2);

    public int MaximumKeyLength { get; set; } = 1024;

    public int MaximumPayloadBytes { get; set; } = 1024 * 1024;
}