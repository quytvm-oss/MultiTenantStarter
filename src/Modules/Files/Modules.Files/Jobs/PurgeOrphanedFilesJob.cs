using Hangfire;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

using Modules.Files.Contracts.Enums;

using Modules.Files.Data;

using Storage.Abstractions;

namespace Modules.Files.Jobs;

public class PurgeOrphanedFilesJob
{
    private readonly FilesDbContext _db;
    private readonly IStorageService _storage;
    private readonly ILogger<PurgeOrphanedFilesJob> _logger;
    public PurgeOrphanedFilesJob(FilesDbContext db, IStorageService storage, ILogger<PurgeOrphanedFilesJob> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 600])]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var orphans = await _db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.Status == FileAssetStatus.PendingUpload
                        && f.UploadDeadline != null
                        && f.UploadDeadline < now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (orphans.Count == 0)
        {
            return;
        }

        foreach (var f in orphans)
        {
            try
            {
                await _storage.RemoveAsync(f.StorageKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove orphan storage object {Key}", f.StorageKey);
            }
            // Hard delete (row never reached Available, so soft-delete doesn't apply). FileAsset is ISoftDeletable,
            // so Remove() would become UPDATE IsDeleted=true — we use the bulk ExecuteDelete below to bypass the interceptor instead.
        }

        // Bulk hard delete — bypasses the soft-delete interceptor.
        var ids = orphans.Select(f => f.Id).ToList();
        await _db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => ids.Contains(f.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Purged {Count} orphaned file assets", orphans.Count);
        }
    }
}
