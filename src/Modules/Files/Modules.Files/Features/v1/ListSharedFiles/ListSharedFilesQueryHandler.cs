using System.Collections.ObjectModel;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Files.Contracts.DTOs;
using Modules.Files.Contracts.Enums;

using Modules.Files.Contracts.v1.Queries;
using Modules.Files.Data;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.ListSharedFiles;

public class ListSharedFilesQueryHandler : IQueryHandler<ListSharedFilesQuery, ReadOnlyCollection<FileAssetDto>>
{
    private readonly FilesDbContext _db;
    private readonly IStorageService _storageService;

    // Free-standing tenant files (not bound to a domain entity). Catalog/Tickets/Chat attachments
    // are excluded — their visibility follows the owning entity's access policy, not a share decision.
    private static readonly string[] SharedOwnerTypes = ["MyFiles", "User"];

    public ListSharedFilesQueryHandler(FilesDbContext db, IStorageService storageService)
    {
        _db = db;
        _storageService = storageService;

    }

    public async ValueTask<ReadOnlyCollection<FileAssetDto>> Handle(ListSharedFilesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await _db.FileAssets.AsNoTracking()
        .Where(x => x.Visibility == Visibility.Public && x.Status == FileAssetStatus.Available
            && SharedOwnerTypes.Contains(x.OwnerType))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(f => FileAssetMapper.ToDto(f, _storageService.BuildPublicUrl(f.StorageKey)))
            .ToList()
            .AsReadOnly();
    }

}
