using Core.Domain;

using Microsoft.AspNetCore.Identity;

using Modules.Identity.Domain.Events;

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
    
    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void Activate(string? activityBy = null, string? tenantId = null)
    {
        if (IsActive) return;
        IsActive = true;
        AddDomainEvent(UserActivatedEvent.Create(
            userId: Id, 
            activatedBy: activityBy,
            tenantId: tenantId));
    }

    public void Deactivate(string? deactivatedBy = null, string? reason = null, string? tenantId = null)
    {
        if (!IsActive) return;
        IsActive = false;
        AddDomainEvent(UserDeactivatedEvent.Create(
            userId: Id,
            deactivatedBy: deactivatedBy,
            reason: reason,
            tenantId: tenantId));
    }
    
    /// <summary>Records UserRegisteredEvent. Call after user creation.</summary>
    public void RecordRegistered(string? tenantId = null, string? source = null)
    {
        AddDomainEvent(UserRegisteredEvent.Create(
            userId: Id,
            email: Email ?? string.Empty,
            source: source,
            firstName: FirstName,
            lastName: LastName,
            tenantId: tenantId));
    }
    
    /// <summary>Records UserRegisteredEvent. Call after user creation.</summary>
    public void RequestEmailConfirmation(string? tenantId, string origin)
    {
        AddDomainEvent(EmailConfirmationRequestedEvent.Create(
            userId: Id,
            email: Email ?? string.Empty,
            origin: origin,
            tenantId: tenantId!));
    }
}