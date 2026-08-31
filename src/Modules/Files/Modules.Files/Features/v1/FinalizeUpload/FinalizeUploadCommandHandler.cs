using System.Diagnostics;
using System.Net;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Files.Contracts.DTOs;
using Modules.Files.Contracts.Enums;
using Modules.Files.Contracts.Events;

using Modules.Files.Contracts.v1.Commands;
using Modules.Files.Data;
using Modules.Files.Services;

using Rebus.Bus;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.FinalizeUpload;

public class FinalizeUploadCommandHandler : ICommandHandler<FinalizeUploadCommand, FileAssetDto>
{
    private readonly FilesDbContext _db;
    private readonly IStorageService _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IFileScanner _fileScanner;
    private readonly IBus _bus;
    public FinalizeUploadCommandHandler(FilesDbContext db, IStorageService storage, ICurrentUser currentUser, IFileScanner fileScanner, IBus bus)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
        _fileScanner = fileScanner;
        _bus = bus;
    }
    public async ValueTask<FileAssetDto> Handle(FinalizeUploadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var tenantId = _currentUser.GetTenantId() ?? throw new UnauthorizedException("invalid tenant");
        var userId = _currentUser.GetUserId().ToString();

        var asset = await _db.FileAssets
            .FirstOrDefaultAsync(f => f.Id == command.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        if (!string.Equals(asset.CreatedByUserId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("not your pending file");
        }
        if (asset.Status != FileAssetStatus.PendingUpload)
        {
            throw new CustomException("file already finalized", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        var head = await _storage.HeadObjectAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("upload not received", (IEnumerable<string>?)null, HttpStatusCode.Conflict);

        // Allow declared+1% slack (S3 may differ slightly on multipart). Reject larger sizes.
        var maxAllowed = asset.SizeBytes + Math.Max(1024L, asset.SizeBytes / 100);
        if (head.SizeBytes > maxAllowed)
        {
            await _storage.RemoveAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false);
            _db.FileAssets.Remove(asset);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new CustomException(
                $"uploaded size ({head.SizeBytes}) exceeds declared ({asset.SizeBytes})",
                (IEnumerable<string>?)null,
                HttpStatusCode.BadRequest);
        }

        if (!string.Equals(head.ContentType, asset.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            await _storage.RemoveAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false);
            _db.FileAssets.Remove(asset);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new CustomException(
                "uploaded content-type mismatch",
                (IEnumerable<string>?)null,
                HttpStatusCode.BadRequest);
        }

        var scanResult = await _fileScanner.ScanAsync(asset.StorageKey, cancellationToken).ConfigureAwait(false);
        asset.MarkAvailable(head.SizeBytes, scanResult);

        // Debit quota with the actual bytes. Refunded on hard purge by PurgeDeletedFilesJob.
        //await quotas.RecordAsync(tenantId, QuotaResource.StorageBytes, head.SizeBytes, cancellationToken).ConfigureAwait(false);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        await _bus.Send(new FileFinalizedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenantId,
            CorrelationId: correlationId,
            Source: "Files",
            FileAssetId: asset.Id,
            OwnerType: asset.OwnerType,
            OwnerId: asset.OwnerId,
            ContentType: asset.ContentType,
            SizeBytes: asset.SizeBytes,
            FinalStatus: (int)asset.Status)).ConfigureAwait(false);

        return FileAssetMapper.ToDto(asset);
    }

}
