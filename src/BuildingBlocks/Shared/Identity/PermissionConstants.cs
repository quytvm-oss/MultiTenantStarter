namespace Shared.Identity;

public class PermissionConstants
{
    private static readonly List<Permission> _all = new();
    
    public const string RequiredPermissionPolicyName = "RequiredPermission";
    
    /// <summary>
    /// Registers permissions from a module/component. Duplicates (by Name) are skipped.
    /// </summary>
    public static void Register(IEnumerable<Permission> additionalPermissions)
    {
        ArgumentNullException.ThrowIfNull(additionalPermissions);
        _all.AddRange(from permission in additionalPermissions
            where !_all.Any(p => p.Name == permission.Name)
            select permission);
    }

    public static IReadOnlyList<Permission> All => _all.AsReadOnly();
    public static IReadOnlyList<Permission> Root => [.. _all.Where(p => p.IsRoot)];
    public static IReadOnlyList<Permission> Admin => [.. _all.Where(p => !p.IsRoot)];
    public static IReadOnlyList<Permission> Basic => [.. _all.Where(p => p.IsBasic)];
}