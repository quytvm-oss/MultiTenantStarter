using Mediator;

using Modules.Files.Contracts.DTOs;

namespace Modules.Files.Contracts.v1.Queries;

public sealed record GetFileMetadataQuery(Guid FileAssetId) : IQuery<FileAssetDto>;