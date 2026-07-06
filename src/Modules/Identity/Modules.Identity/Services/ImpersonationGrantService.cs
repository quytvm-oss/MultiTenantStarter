using Microsoft.Extensions.Caching.Hybrid;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;

namespace Modules.Identity.Services;

public class ImpersonationGrantService(
    IdentityDbContext db,
    HybridCache cache,
    TimeProvider timeProvider) : IImpersonationGrantService
{
    public Task<ImpersonationGrantDto> CreateAsync(CreateGrantInput input, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ImpersonationGrantDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ImpersonationGrantDto?> MarkEndedByJtiAsync(string jti, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ImpersonationGrantDto> RevokeAsync(Guid id, string revokedByUserId, string? revokedByUserName, string? reason,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsRevokedOrEndedAsync(string jti, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ImpersonationGrantDto>> ListAsync(ImpersonationGrantStatus? status, string? impersonatedTenantId, string? actorUserId, int take,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    #region internals

    

    #endregion
}