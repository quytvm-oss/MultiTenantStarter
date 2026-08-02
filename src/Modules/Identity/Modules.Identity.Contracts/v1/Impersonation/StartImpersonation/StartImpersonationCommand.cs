using Mediator;

namespace Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

// DurationMinutes: requested token lifetime, capped server-side at
// StartImpersonationCommandValidator.MaxImpersonationMinutes (60); null → JwtOptions.AccessTokenMinutes.
public record StartImpersonationCommand(
    string TargetUserId,
    string TargetTenantId,
    string? Reason,
    int? DurationMinutes = null) : ICommand<ImpersonationResponse>;


public sealed record ImpersonationResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string ActorUserId,
    string ActorTenantId,
    string ImpersonatedUserId,
    string ImpersonatedTenantId);