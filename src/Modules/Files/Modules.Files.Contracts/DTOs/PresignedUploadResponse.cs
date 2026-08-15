namespace Modules.Files.Contracts.DTOs;

public sealed record PresignedUploadResponse(
    Guid FileAssetId,
    Uri UploadUrl,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    DateTimeOffset ExpiresAt);