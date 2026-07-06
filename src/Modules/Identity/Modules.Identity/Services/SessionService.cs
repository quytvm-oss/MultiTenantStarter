using Core.Context;
using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.Logging;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;

using Shared.Multitenancy;

namespace Modules.Identity.Services;

public class SessionService : ISessionService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    private readonly ILogger<SessionService> _logger;
    private readonly TimeProvider _timeProvider;
    //private readonly Parser _uaParser;

    public SessionService(
        IdentityDbContext db,
        ICurrentUser currentUser,
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        ILogger<SessionService> logger,
        TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _logger = logger;
        _timeProvider = timeProvider;
        //_uaParser = Parser.GetDefault();
    }
    public Task<UserSessionDto> CreateSessionAsync(string userId, string refreshTokenHash, string ipAddress, string userAgent, DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserSessionDto>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserSessionDto>> GetUserSessionsForAdminAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(List<UserSessionDto> Items, long TotalCount)> GetTenantSessionsAsync(bool includeInactive, string? search, int skip, int take,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeSessionAsync(Guid sessionId, string revokedBy, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> RevokeAllSessionsAsync(string userId, string revokedBy, Guid? exceptSessionId = null, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> RevokeAllSessionsForAdminAsync(string userId, string revokedBy, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeSessionForAdminAsync(Guid sessionId, string revokedBy, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateSessionActivityAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateSessionRefreshTokenAsync(string oldRefreshTokenHash, string newRefreshTokenHash, DateTime newExpiresAt,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateSessionAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Guid?> GetSessionIdByRefreshTokenAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #region internals

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(_multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("Invalid tenant");
        }
    }

    #endregion
}