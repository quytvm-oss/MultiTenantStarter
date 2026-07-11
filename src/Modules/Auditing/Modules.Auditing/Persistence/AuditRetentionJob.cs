using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;

namespace Modules.Auditing.Persistence;

public class AuditRetentionJob(
    AuditDbContext db,
    AuditRetentionOptions opts,
    TimeProvider timeProvider,
    ILogger<AuditRetentionJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        if (!opts.Enabled)
        {
            logger.LogInformation("[Auditing] retention job skipped (Enabled=false).");
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        long total = 0;
        total += await SweepAsync(AuditEventType.Activity, now.AddDays(-opts.ActivityRetentionDays), ct).ConfigureAwait(false);
        total += await SweepAsync(AuditEventType.EntityChange, now.AddDays(-opts.EntityChangeRetentionDays), ct).ConfigureAwait(false);
        total += await SweepAsync(AuditEventType.Security, now.AddDays(-opts.SecurityRetentionDays), ct).ConfigureAwait(false);
        total += await SweepAsync(AuditEventType.Exception, now.AddDays(-opts.ExceptionRetentionDays), ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Auditing] retention job purged {Total} rows.", total);
        }
    }

    private async Task<long> SweepAsync(AuditEventType eventType, DateTime cutoffUtc, CancellationToken ct)
    {
        long swept = 0;
        var typeId = (int)eventType;
        var batchSize = Math.Max(100, opts.DeleteBatchSize);

        while (!ct.IsCancellationRequested)
        {
            var deleted = await db.AuditRecords
                .Where(a => a.EventType == typeId
                            && a.OccurredAtUtc < cutoffUtc
                            && db.AuditRecords
                                .Where(b => b.EventType == typeId && b.OccurredAtUtc < cutoffUtc)
                                .OrderBy(b => b.OccurredAtUtc)
                                .Select(b => b.Id)
                                .Take(batchSize)
                                .Contains(a.Id))
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);
            
            swept += deleted;
            if (deleted < batchSize) break;
        }
        
        if (swept > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Auditing] purged {Count} {EventType} events older than {Cutoff:o}.",
                swept, eventType, cutoffUtc);
        }
        return swept;
    }
}