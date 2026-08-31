using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Files.Contracts.v1.Commands;
using Modules.Files.Data;

namespace Modules.Files.Features.v1.RestoreFile;

public class RestoreFileCommandHandler : ICommandHandler<RestoreFileCommand, Unit>
{
    private readonly FilesDbContext _db;

    public RestoreFileCommandHandler(FilesDbContext db)
    {
        _db = db;
    }


    public async ValueTask<Unit> Handle(RestoreFileCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // IgnoreQueryFilters because the SoftDelete filter would otherwise hide the row.
        var file = await _db.FileAssets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        if (!file.IsDeleted)
        {
            return Unit.Value; // idempotent — already live
        }

        file.Restore();
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

}
