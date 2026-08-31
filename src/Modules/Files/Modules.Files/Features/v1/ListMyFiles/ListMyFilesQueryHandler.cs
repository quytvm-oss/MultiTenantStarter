using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Files.Contracts.DTOs;
using Modules.Files.Contracts.Enums;

using Modules.Files.Contracts.v1.Queries;
using Modules.Files.Data;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.ListMyFiles;

public class ListMyFilesQueryHandler : IQueryHandler<ListMyFilesQuery, IReadOnlyCollection<FileAssetDto>>
{
    private readonly FilesDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStorageService _storageService;

    public ListMyFilesQueryHandler(FilesDbContext db, ICurrentUser currentUser, IStorageService storageService)
    {
        _db = db;
        _currentUser = currentUser;
        _storageService = storageService;
    }

    public async ValueTask<IReadOnlyCollection<FileAssetDto>> Handle(ListMyFilesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = _currentUser.GetUserId().ToString();
        if (string.IsNullOrEmpty(userId) || userId == Guid.Empty.ToString())
        {
            throw new UnauthorizedException("no current user");
        }

        var page = Math.Max(1, query.PageSize);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var rows = await _db.FileAssets.Where(x => x.CreatedByUserId == userId && x.Status == FileAssetStatus.Available)
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAtUtc)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        // Seed publicUrl for public files so the preview dialog can paint the image
        // immediately from the list data, without waiting on a metadata refetch to mint it.
        return rows
            .Select(f => FileAssetMapper.ToDto(
                f,
                f.Visibility == Visibility.Public ? _storageService.BuildPublicUrl(f.StorageKey) : null))
            .ToList()
            .AsReadOnly();
    }

}
