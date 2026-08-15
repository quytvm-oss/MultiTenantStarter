using Modules.Files.Contracts.Enums;

namespace Modules.Files.Contracts.DTOs;

/// <summary>
/// Wire shape returned by every Files-module read endpoint.
/// <para>
/// <c>CreatedByUserId</c> is the user id that uploaded the file. Used by the SPA both to
/// display an "Uploaded by" attribution and to decide whether the caller should see
/// destructive affordances (Delete, Change visibility) on a file they don't own.
/// </para>
/// </summary>
public record FileAssetDto(
    Guid Id,
    string OwnerType,
    Guid? OwnerId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Visibility Visibility,
    FileAssetStatus Status,
    int ScanStatus,
    DateTime CreatedAtUtc,
    string? PublicUrl,
    string CreatedByUserId = "",
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);