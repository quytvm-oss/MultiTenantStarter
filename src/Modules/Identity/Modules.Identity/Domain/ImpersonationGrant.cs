using Core.Domain;

namespace Modules.Identity.Domain;

public class ImpersonationGrant : IGlobalEntity
{
    public Guid Id { get; private set; }

    public string Jit { get; private set; } = default!;

    public string ActorUserId { get; private set; } = default!;

    public string? ActorUserName { get; private set; }
    
    public string ActorTenantId { get; private set; } = default!;

    public string ImpersonatedUserId { get; private set; } = default!;

    public string? ImpersonatedUserName { get; private set; }

    public string ImpersonatedTenantId { get; private set; } = default!;

    public string Reason { get; private set; } = string.Empty;
    
    public DateTime StartedAtUtc { get; private set; }
    
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>Set when the operator clicks End-impersonation. Tokens still expire naturally even if null.</summary>
    public DateTime? EndedAtUtc { get; private set; }

    /// <summary>Set when an operator revokes the grant explicitly. Distinct from EndedAtUtc.</summary>
    public DateTime? RevokedAtUtc { get; private set; }
    
    public string? RevokedByUserId { get; private set; }
    
    public string? RevokedByUserName { get; private set; }
    
    public string? RevokeReason { get; private set; }

    public string? ClientId { get; private set; }
    
    public string? IpAddress { get; private set; }
    
    public string? UserAgent { get; private set; }
    
    private ImpersonationGrant() {}
}