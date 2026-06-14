using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using MessageBus.Contracts;

namespace MessageBus.Subscribes;

public sealed class SubscriptionMatcherCache
{
    private readonly ISubscribeBusBuilder _builder;
    private readonly ConcurrentDictionary<string, IReadOnlyList<ConsumerExecutorRegistration>> _entries;
    private readonly ConcurrentDictionary<SubscriptionRoute, IReadOnlyList<ConsumerExecutorRegistration>> _routes;
    private readonly ConcurrentDictionary<string, byte> _groupConcurrent;
    private readonly ConcurrentDictionary<string, IReadOnlyList<RegexExecutorRegistration>> _regexCache;
    private readonly object _sync = new();

    private bool _loaded;

    public SubscriptionMatcherCache(ISubscribeBusBuilder builder)
    {
        _builder = builder;
        _entries = new ConcurrentDictionary<string, IReadOnlyList<ConsumerExecutorRegistration>>
            (StringComparer.OrdinalIgnoreCase);
        _routes = new ConcurrentDictionary<SubscriptionRoute, IReadOnlyList<ConsumerExecutorRegistration>>
            (SubscriptionRouteComparer.Instance);
        _groupConcurrent = new ConcurrentDictionary<string, byte>
            (StringComparer.OrdinalIgnoreCase);
        _regexCache = new ConcurrentDictionary<string, IReadOnlyList<RegexExecutorRegistration>>
            (StringComparer.OrdinalIgnoreCase);
    }

    public ConcurrentDictionary<string, IReadOnlyList<ConsumerExecutorRegistration>> GetCandidatesMethodsOfGroupNameGrouped()
    {
        EnsureLoaded();
        return _entries;
    }

    public byte GetGroupConcurrentLimit(string group)
    {
        EnsureLoaded();
        return _groupConcurrent.GetValueOrDefault(NormalizeGroupKey(group), (byte)1);
    }

    public IReadOnlyList<string> GetAllTopics()
    {
        EnsureLoaded();

        return _entries.Values
            .SelectMany(x => x)
            .Select(x => x.Descriptor.RoutingKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public bool TryGetTopicExecutors(string routingKey, string groupName, [NotNullWhen(true)] out IReadOnlyList<ConsumerExecutorRegistration>? matches)
    {
        ArgumentNullException.ThrowIfNull(routingKey);
        EnsureLoaded();

        matches = null;

        var groupKey = NormalizeGroupKey(groupName);
        var route = new SubscriptionRoute(groupKey, routingKey.Trim());
        
        if (_routes.TryGetValue(route, out var exactMatches))
        {
            matches = exactMatches;
            return true;
        }
        
        if (!_entries.ContainsKey(groupKey))
            return false;

        var regexMatches = GetRegexEntries(groupKey)
            .Where(x => Regex.IsMatch(routingKey, x.Pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            .Select(x => x.Registration)
            .ToList();

        if (regexMatches.Count == 0)
            return false;

        matches = regexMatches.AsReadOnly();
        return true;
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (_sync)
        {
            if (_loaded)
            {
                return;
            }

            _builder.Build();

            foreach (var group in _builder.Registrations.GroupBy(x => NormalizeGroupKey(x.Descriptor.Group)))
            {
                var entries = group.ToList().AsReadOnly();
                _entries.TryAdd(group.Key, entries);

                var concurrentLimit = group.Sum(x => x.Descriptor.GroupConcurrent);
                _groupConcurrent.TryAdd(group.Key, (byte)Math.Clamp(concurrentLimit, 1, byte.MaxValue));
            }

            foreach (var route in _builder.Registrations.GroupBy(
                x => new SubscriptionRoute(NormalizeGroupKey(x.Descriptor.Group), x.Descriptor.RoutingKey),
                SubscriptionRouteComparer.Instance))
            {
                _routes.TryAdd(route.Key, route.ToList().AsReadOnly());
            }

            _loaded = true;
        }
    }

    private IReadOnlyList<RegexExecutorRegistration> GetRegexEntries(string groupName)
    {
        var groupKey = NormalizeGroupKey(groupName);

        return _regexCache.GetOrAdd(groupKey, key =>
        {
            if (!_entries.TryGetValue(key, out var groupEntries))
            {
                return [];
            }

            return groupEntries
                .Select(x => new RegexExecutorRegistration(
                    WildcardToRegex(x.Descriptor.RoutingKey),
                    x))
                .ToList()
                .AsReadOnly();
        });
    }

    private static string NormalizeGroupKey(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ? string.Empty : group.Trim();
    }

    private static string WildcardToRegex(string routingKey)
    {
        return "^" + Regex.Escape(routingKey)
            .Replace("\\*", "[^.]+", StringComparison.Ordinal)
            .Replace("\\#", ".*", StringComparison.Ordinal) + "$";
    }

    private sealed record RegexExecutorRegistration(
        string Pattern,
        ConsumerExecutorRegistration Registration);
}