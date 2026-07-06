using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.Services;

public interface IImpersonationGrantService
{
    Task<ImpersonationGrantDto> CreateAsync(CreateGrantInput input, CancellationToken ct = default);

    Task<ImpersonationGrantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<ImpersonationGrantDto?> MarkEndedByJtiAsync(string jti, CancellationToken ct = default);

    Task<ImpersonationGrantDto> RevokeAsync(
        Guid id,
        string revokedByUserId,
        string? revokedByUserName,
        string? reason,
        CancellationToken ct = default);
    
    Task<bool> IsRevokedOrEndedAsync(string jti, CancellationToken ct = default);

    Task<IReadOnlyList<ImpersonationGrantDto>> ListAsync(
        ImpersonationGrantStatus? status,
        string? impersonatedTenantId,
        string? actorUserId,
        int take,
        CancellationToken ct = default);
}