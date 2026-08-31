using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Files.Contracts;

using Modules.Files.Contracts.DTOs;
using Modules.Files.Contracts.Enums;

using Modules.Files.Contracts.v1.Queries;
using Modules.Files.Data;
using Modules.Files.Services;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.GetFileMetadata;

public class GetFileMetadataQueryHandler : IQueryHandler<GetFileMetadataQuery, FileAssetDto>
{
    private readonly FilesDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly FileAccessPolicyRegistry _policies;
    private readonly IStorageService _storageService;

    public GetFileMetadataQueryHandler(FilesDbContext db, ICurrentUser currentUser, IStorageService storageService, FileAccessPolicyRegistry policies)
    {
        _db = db;
        _currentUser = currentUser;
        _storageService = storageService;
        _policies = policies;

    }


    public async ValueTask<FileAssetDto> Handle(GetFileMetadataQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var f = await _db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var userId = _currentUser.GetUserId().ToString();
        var policy = _policies.Resolve(f.OwnerType)
            ?? throw new NotFoundException("file not found"); // don't leak existence on missing policy

        var ctx = new FileAccessContext(f.Id, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanReadAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("file not found");
        }

        // Public files get a durable URL safe to persist long-term, while private files mint a
        // short-lived presigned GET on demand via the auth-gated url endpoint.
        var publicUrl = f.Visibility == Visibility.Public
            ? _storageService.BuildPublicUrl(f.StorageKey)
            : null;

        return FileAssetMapper.ToDto(f, publicUrl);
    }

}
