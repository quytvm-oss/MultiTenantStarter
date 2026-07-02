using System.Collections.Immutable;

namespace Modules.Identity.Caching;

internal sealed record PermissionSet(ImmutableArray<string> Values)
{
    public static PermissionSet Empty { get; } = new(ImmutableArray<string>.Empty);

    public bool Contains(string permission) => Values.Contains(permission);
};