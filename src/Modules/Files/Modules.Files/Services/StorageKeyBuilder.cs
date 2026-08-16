using System.Text.RegularExpressions;

namespace Modules.Files.Services;

/// <summary>
/// Builds canonical storage keys for the Files module:
///   tenants/{tenantId}/{ownerType-lower}/{yyyy}/{MM}/{fileAssetId:N}/{sanitized-filename}.
/// Tenant prefix is mandatory defense-in-depth even when the bucket is already tenant-scoped via
/// connection-string-per-tenant — keys carry the prefix so cross-tenant key-collision is impossible
/// even if a future deployment puts multiple tenants in one bucket.
/// </summary>
public static partial class StorageKeyBuilder
{
    [GeneratedRegex(@"[^a-zA-Z0-9_\.-]")]
    private static partial Regex UnsafeChars();

    public static string Build(string tenantId, string ownerType, Guid fileAssetId, string fileName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        
        var lowerOwner = ownerType.ToLowerInvariant();
        var safe = Sanitize(fileName);
        return $"tenants/{tenantId}/{lowerOwner}/{now:yyyy}/{now:MM}/{fileAssetId:N}/{safe}";
    }

    public static string Sanitize(string fileNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNames);
        return UnsafeChars().Replace(fileNames, "_");
    }
}