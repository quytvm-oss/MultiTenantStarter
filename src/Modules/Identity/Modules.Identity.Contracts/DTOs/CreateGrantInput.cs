namespace Modules.Identity.Contracts.DTOs;

public sealed record CreateGrantInput(
    string Jti,
    string ActorUserId,
    string? ActorUserName,
    string ActorTenantId,
    string ImpersonatedUserId,
    string? ImpersonatedUserName,
    string ImpersonatedTenantId,
    string Reason,
    DateTime StartedAtUtc,
    DateTime ExpiresAtUtc,
    string? ClientId,
    string? IpAddress,
    string? UserAgent);