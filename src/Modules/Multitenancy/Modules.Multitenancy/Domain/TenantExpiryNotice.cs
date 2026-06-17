namespace Modules.Multitenancy.Domain;

public class TenantExpiryNotice 
{
    public Guid Id { get; private set; }

    public string TenantId { get; private set; } = default!;

    public string NoticeType { get; private set; } = default!;

    public DateTime ValidUptoUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    
    private TenantExpiryNotice() {}

    public static TenantExpiryNotice Record(string tenantId, string noticeType, DateTime validUptoUtc,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(noticeType);

        return new TenantExpiryNotice()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            NoticeType = noticeType,
            ValidUptoUtc = DateTime.SpecifyKind(validUptoUtc, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }
}