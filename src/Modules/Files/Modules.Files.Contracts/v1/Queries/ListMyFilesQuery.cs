using Mediator;

using Modules.Files.Contracts.DTOs;

namespace Modules.Files.Contracts.v1.Queries;

public record ListMyFilesQuery(int PageIndex = 1, int PageSize = 10) : IQuery<IReadOnlyCollection<FileAssetDto>>;