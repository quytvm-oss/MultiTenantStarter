namespace Modules.Files.Contracts;

/// <summary>
/// Minimal projection of a FileAsset passed to <see cref="IFileAccessPolicy"/> methods so owning
/// modules can make access decisions without depending on the Files module runtime.
/// </summary>
/// <param name="FileAssetId">FileAsset identity.</param>
/// <param name="OwnerType">OwnerType value.</param>
/// <param name="OwnerId">OwnerId value, or null when the file isn't bound to an owner (e.g. MyFiles).</param>
/// <param name="CreatedByUserId">User who uploaded the file.</param>
/// <param name="Visibility">0 = Public, 1 = Private (matches the runtime enum int).</param>
public record FileAccessContext(
    Guid FileAssetId,
    string OwnerType,
    Guid? OwnerId,
    string CreatedByUserId,
    int Visibility);