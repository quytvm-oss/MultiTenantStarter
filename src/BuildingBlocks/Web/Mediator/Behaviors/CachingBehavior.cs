using Mediator;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Web.Mediator.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(1);

    private readonly HybridCache _hybridCache;
    private readonly IEnumerable<ICachePolicy<TRequest, TResponse>> _cachePolicies;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        HybridCache hybridCache,
        IEnumerable<ICachePolicy<TRequest, TResponse>> cachePolicies,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _hybridCache = hybridCache;
        _cachePolicies = cachePolicies;
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var cachePolicy = _cachePolicies.FirstOrDefault();
        if (cachePolicy is null)
            return await next(message, cancellationToken);

        var cacheKey = cachePolicy.GetCacheKey(message);
        var expiry = cachePolicy.AbsoluteExpirationRelativeToNow ?? DefaultExpiration;
        var tags = cachePolicy.Tags?.ToArray();

        _logger.LogDebug("Cache lookup for {Request} | Key: {Key}",
            typeof(TRequest).Name, cacheKey);

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            state: (message, next),
            factory: static async (state, ct) => await state.next(state.message, ct),
            options: new HybridCacheEntryOptions
            {
                Expiration = expiry,
                LocalCacheExpiration = expiry < TimeSpan.FromMinutes(5)
                    ? expiry
                    : TimeSpan.FromMinutes(5),
            },
            tags: tags,
            cancellationToken: cancellationToken
        );
    }
}