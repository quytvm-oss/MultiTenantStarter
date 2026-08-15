using Mediator;

using Modules.Files.Contracts.DTOs;

using Shared.Persistence;

namespace Modules.Files.Contracts.v1.Queries;

public record ListTrashedFilesQuery(int PageIndex = 1, int PageSize = 10)
    : IQuery<PagedResponse<FileAssetDto>>;