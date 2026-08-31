using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Files.Contracts;

using Modules.Files.Contracts.DTOs;
using Modules.Files.Contracts.Enums;

using Modules.Files.Contracts.v1.Commands;
using Modules.Files.Data;
using Modules.Files.Services;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.ChangeVisibility;

public class ChangeFileVisibilityCommandHandler : ICommandHandler<ChangeFileVisibilityCommand, FileAssetDto>
{
    private readonly FilesDbContext _db;
    private readonly IStorageService _storage;
    private readonly ICurrentUser _currentUser;
    private readonly FileAccessPolicyRegistry _policy;
    public ChangeFileVisibilityCommandHandler(FilesDbContext db, IStorageService storage, ICurrentUser currentUser, FileAccessPolicyRegistry policy = null)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
        _policy = policy;

    }

    public async ValueTask<FileAssetDto> Handle(ChangeFileVisibilityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Visibility is not (Visibility.Public or Visibility.Private))
        {
            throw new CustomException(
                $"Unknown visibility value '{command.Visibility}'.",
                errors: null,
                System.Net.HttpStatusCode.BadRequest);
        }

        var file = await _db.FileAssets
            .FirstOrDefaultAsync(x => x.Id == command.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var userId = _currentUser.GetUserId().ToString();
        var policy = _policy.Resolve(file.OwnerType)
            ?? throw new ForbiddenException("no policy");
        var ctx = new FileAccessContext(file.Id, file.OwnerType, file.OwnerId, file.CreatedByUserId, (int)file.Visibility);
        if (!await policy.CanChangeVisibilityAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("not allowed to change this file's visibility");
        }

        file.ChangeVisibility(command.Visibility);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var publicUrl = file.Visibility == Visibility.Public
            ? _storage.BuildPublicUrl(file.StorageKey)
            : null;
        return FileAssetMapper.ToDto(file, publicUrl);
    }

}
