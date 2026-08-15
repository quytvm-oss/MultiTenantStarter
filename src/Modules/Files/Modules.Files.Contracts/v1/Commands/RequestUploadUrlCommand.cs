using Mediator;

using Modules.Files.Contracts.DTOs;
using Modules.Files.Contracts.Enums;

namespace Modules.Files.Contracts.v1.Commands;

public sealed record RequestUploadUrlCommand(
    string OwnerType,
    Guid? OwnerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Visibility Visibility,
    string Category) : ICommand<PresignedUploadResponse>;