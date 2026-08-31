using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Files.Contracts;

using Modules.Files.Contracts.v1.Commands;
using Modules.Files.Data;
using Modules.Files.Services;

namespace Modules.Files.Features.v1.DeleteFile;

public class DeleteFileCommandHandler : ICommandHandler<DeleteFileCommand, Unit>
{
    private readonly FilesDbContext _db;
    private readonly FileAccessPolicyRegistry _policies;
    private readonly ICurrentUser _currentUser;
    public DeleteFileCommandHandler(FilesDbContext db, FileAccessPolicyRegistry policies, ICurrentUser currentUser)
    {
        _db = db;
        _policies = policies;
        _currentUser = currentUser;
    }
    public async ValueTask<Unit> Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var file = await _db.FileAssets
            .FirstOrDefaultAsync(x => x.Id == command.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var userId = _currentUser.GetUserId().ToString();
        var policy = _policies.Resolve(file.OwnerType)
            ?? throw new ForbiddenException("no policy");
        var ctx = new FileAccessContext(file.Id, file.OwnerType, file.OwnerId, file.CreatedByUserId, (int)file.Visibility);
        if (!await policy.CanDeleteAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("not allowed to delete this file");
        }

        // Soft-delete: AuditableEntitySaveChangesInterceptor sets IsDeleted/DeletedOnUtc/DeletedBy on
        // Remove() for ISoftDeletable; byte purge runs later via PurgeDeletedFilesJob post-retention.
        _db.FileAssets.Remove(file);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

}
