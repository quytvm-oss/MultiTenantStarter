using Core.Domain;

using Modules.Files.Contracts.Enums;

namespace Modules.Files.Domain;

public class FileAsset : AggregateRoot<Guid>, ISoftDeletable
{
    public string OwnerType { get; private set; } = default!;

    public Guid? OwnerId { get; private set; }

    public string FileName { get; private set; } = default!;

    public string OriginalFileName { get; private set; } = default!;
    
    public string ContentType { get; private set; } = default!;
    
    public long SizeBytes { get; private set; }
    
    public string StorageKey { get; private set; } = default!;

    public Visibility Visibility { get; private set; }

    public FileAssetStatus Status { get; private set; }

    public ScanStatus ScanStatus { get; private set; }

    public DateTimeOffset? UploadDeadline { get; private set; }

    public string CreatedByUserId { get; private set; } = default!;
    
    public DateTime CreatedAtUtc { get; private set; }
    
    public DateTime? UpdatedAtUtc { get; private set; }
    
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    
    public FileAsset(){}

    public static FileAsset CreatePending(
        Guid id,
        string ownerType,
        Guid? ownerId,
        string originalFileName,
        string sanitizedFileName,
        string contentType,
        long declaredSizeBytes,
        string storageKey,
        Visibility visibility,
        string createdByUserId,
        DateTimeOffset uploadDeadline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sanitizedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        if (declaredSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(declaredSizeBytes), "Declared size must be positive.");
        }
        
        return new FileAsset
        {
            Id = id == Guid.Empty ? Guid.CreateVersion7() : id,
            OwnerType = ownerType,
            OwnerId = ownerId,
            OriginalFileName = originalFileName,
            FileName = sanitizedFileName,
            ContentType = contentType,
            SizeBytes = declaredSizeBytes,
            StorageKey = storageKey,
            Visibility = visibility,
            Status = FileAssetStatus.PendingUpload,
            ScanStatus = ScanStatus.NotScanned,
            UploadDeadline = uploadDeadline,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}