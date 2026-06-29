using System.Diagnostics;

using Microsoft.Extensions.Caching.Hybrid;

namespace Caching.V2;

public class ObservableHybridCache : HybridCache
{
    private readonly HybridCache _inner;

    public ObservableHybridCache(HybridCache inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public override async ValueTask<T> GetOrCreateAsync<TState, T>(
        string key, TState state, 
        Func<TState, CancellationToken, 
            ValueTask<T>> factory, 
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var activity = CachingTelemetry.ActivitySource.StartActivity("cache.get_or_create",ActivityKind.Internal);
        activity?.SetTag("cache.system", "hybrid");
        activity?.SetTag("cache.key", key);
        
        // Wrap the factory so we can record hit/miss and factory duration without allocating a
        // closure over caller state — the caller's state flows through the TState parameter.
        var wrappedState = new FactoryWrapperState<TState, T>(state, factory, invoked: false);
        var wrapperBox = new StrongBox<FactoryWrapperState<TState, T>>(wrappedState);

        T result;
        try
        {
            result = await _inner.GetOrCreateAsync(
                key,
                wrapperBox,
                static async (box, ct) =>
                {
                    var sw = ValueStopwatch.StartNew();
                    box.Value.Invoked = true;
                    try
                    {
                        return await box.Value.Factory(box.Value.State, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        CachingTelemetry.FactoryDurationMs.Record(sw.ElapsedMilliseconds);
                    }
                },
                options,
                tags, 
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }

        if (wrapperBox.Value.Invoked)
        {
            CachingTelemetry.Misses.Add(1);
            activity?.SetTag("cache.hit", false);
        }
        else
        {
            CachingTelemetry.Hits.Add(1);
            activity?.SetTag("cache.hit", true);
        }

        return result;
    }

    public override ValueTask SetAsync<T>(string key, T value, 
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        using var activity = CachingTelemetry.ActivitySource.StartActivity("cache.set",ActivityKind.Internal);
        activity?.SetTag("cache.system", "hybrid");
        activity?.SetTag("cache.key", key);
        
        return _inner.SetAsync(key, value, options, tags, cancellationToken);
    }

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = new CancellationToken())
    {
        using var activity = CachingTelemetry.ActivitySource.StartActivity(
            "cache.remove",
            ActivityKind.Internal);
        activity?.SetTag("cache.system", "hybrid");
        activity?.SetTag("cache.key", key);
        CachingTelemetry.Invalidations.Add(1);
        
        return _inner.RemoveAsync(key, cancellationToken);
    }
    
    public override ValueTask RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        CachingTelemetry.Invalidations.Add(1);
        return _inner.RemoveAsync(keys, cancellationToken);
    }

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = new CancellationToken())
    {
        using var activity = CachingTelemetry.ActivitySource.StartActivity(
            "cache.remove_by_tag",
            ActivityKind.Internal);
        activity?.SetTag("cache.system", "hybrid");
        activity?.SetTag("cache.tag", tag);
        CachingTelemetry.Invalidations.Add(1);
        
        return _inner.RemoveByTagAsync(tag, cancellationToken);
    }


    #region internalclass
     // ---------------
     
     // State flows through TState so we avoid a per-call closure capturing the surrounding
     // ObservableHybridCache instance. The StrongBox is a single-field heap allocation used
     // to observe the "factory invoked?" flag after the inner call returns — unavoidable
     // because HybridCache does not surface hit/miss directly.
     private struct FactoryWrapperState<TState, T>
     {
         public TState State;
         public Func<TState, CancellationToken, ValueTask<T>> Factory;
         public bool Invoked;

         public FactoryWrapperState(TState state, Func<TState, CancellationToken, ValueTask<T>> factory, bool invoked)
         {
             State = state;
             Factory = factory;
             Invoked = invoked;
         }
     }
     
     /// <summary>Minimal reference-type box so the struct state can be observed post-call.</summary>
     private sealed class StrongBox<T>
     {
         public T Value;
         
         public StrongBox(T value) => Value = value;
     }
     
     /// <summary>Struct-based stopwatch to avoid the per-call <see cref="Stopwatch"/> allocation.</summary>
     private readonly struct ValueStopwatch
     {
         private static readonly double TimestampToMs = 1000.0 / Stopwatch.Frequency;
         private readonly long _start;

         public double ElapsedMilliseconds => (Stopwatch.GetTimestamp() - _start) * TimestampToMs;
         
         private ValueStopwatch(long start) => _start = start;
         
         public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());
     }
     #endregion
}