using Core.Domain;

using Microsoft.AspNetCore.Identity;

namespace Modules.Identity.Domain;

public class User : IdentityUser, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public Uri? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime RefreshTokenExpireTime { get; set; }

    public string? ObjectId { get; set; }

    public DateTime LastPasswordChangeDateTime { get; set; } = TimeProvider.System.GetUtcNow().UtcDateTime;
    
    // Navigation property for password history
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}