namespace Modules.Notifications.Contracts.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? Link,
    string Source,
    string MetadataJson,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc);
