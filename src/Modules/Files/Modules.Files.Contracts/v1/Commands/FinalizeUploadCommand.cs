using Mediator;

using Modules.Files.Contracts.DTOs;

namespace Modules.Files.Contracts.v1.Commands;

public sealed record FinalizeUploadCommand(Guid FileAssetId) : ICommand<FileAssetDto>;