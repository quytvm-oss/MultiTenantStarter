namespace MessageBus.Subscribes;

internal sealed class SubscriptionRouteComparer : IEqualityComparer<SubscriptionRoute>
{
    public static readonly SubscriptionRouteComparer Instance = new();

    private SubscriptionRouteComparer()
    {
    }

    public bool Equals(SubscriptionRoute x, SubscriptionRoute y)
    {
        return string.Equals(x.Group, y.Group, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.RoutingKey, y.RoutingKey, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(SubscriptionRoute obj)
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Group),
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.RoutingKey));
    }
}