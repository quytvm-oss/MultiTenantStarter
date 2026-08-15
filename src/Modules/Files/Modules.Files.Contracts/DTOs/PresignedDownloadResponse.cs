namespace Modules.Files.Contracts.DTOs;

public record PresignedDownloadResponse(Uri Url, DateTimeOffset ExpiresAt);