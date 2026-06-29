using Core.Domain;

namespace Modules.Identity.Domain;

public class UserSession : IHasDomainEvents
{
    public Guid Id { get; private set; }

    public string UserId { get; set; } = default!;

    public string RefreshTokenHash { get; private set; } = default!;

    public string IpAddress { get; private set; } = default!;

    public string UserAgent { get; private set; } = default!;

    public string? DeviceType { get; private set; }

    public string? Browser { get; private set; }

    public string? BrowserVersion { get; private set; }
    
    public string? OperatingSystem { get; private set; }

    public string? OsVersion { get;private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime LastActivityAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTime? RevokedAt { get; private set; }
    
    public string? RevokedBy { get; private set; }
    
    public string? RevokedReason { get; private set; }
    
    // Navigation property (init for EF Core materialization)
    public virtual User? User { get; init; }
    
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}