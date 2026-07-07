using Caching.V2;

using Core.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using Modules.Identity.Constant;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

namespace Modules.Identity.Services;

public class ImpersonationGrantService(
    IdentityDbContext db,
    HybridCache cache,
    TimeProvider timeProvider) : IImpersonationGrantService
{
    public async Task<ImpersonationGrantDto> CreateAsync(CreateGrantInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        
        var grant = ImpersonationGrant.Create(
            id: Guid.CreateVersion7(),
            jti: input.Jti,
            actorUserId: input.ActorUserId,
            actorUserName: input.ActorUserName,
            actorTenantId: input.ActorTenantId,
            impersonatedUserId: input.ImpersonatedUserId,
            impersonatedUserName: input.ImpersonatedUserName,
            impersonatedTenantId: input.ImpersonatedTenantId,
            reason: input.Reason,
            startedAtUtc: input.StartedAtUtc,
            expiresAtUtc: input.ExpiresAtUtc,
            clientId: input.ClientId,
            ipAddress: input.IpAddress,
            userAgent: input.UserAgent);
        
        db.ImpersonationGrants.Add(grant);
        await db.SaveChangesAsync(ct); 
        
        await SetCachedStatusAsync(grant, GrantState.Active, ct).ConfigureAwait(false);
        
        return ToDto(grant);
    }

    public async Task<ImpersonationGrantDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var grant = await db.ImpersonationGrants
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct)
            .ConfigureAwait(false);
        return grant is null ? null : ToDto(grant);
    }

    public async Task<ImpersonationGrantDto?> MarkEndedByJtiAsync(string jti, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        var grant = await db.ImpersonationGrants
            .FirstOrDefaultAsync(g => g.Jit == jti, ct)
            .ConfigureAwait(false);
        if (grant is null) return null;
        if (grant.IsTerminal) return ToDto(grant);

        grant.MarkEnded(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SetCachedStatusAsync(grant, GrantState.EndedOrRevoked, ct).ConfigureAwait(false);

        return ToDto(grant);
    }

    public async Task<ImpersonationGrantDto> RevokeAsync(Guid id, string revokedByUserId, string? revokedByUserName, string? reason,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedByUserId);

        var grant = await db.ImpersonationGrants
                        .FirstOrDefaultAsync(g => g.Id == id, ct)
                        .ConfigureAwait(false)
                    ?? throw new NotFoundException("impersonation grant not found");

        if (grant.IsTerminal)
        {
            // Idempotent — surface the existing terminal state to the caller.
            return ToDto(grant);
        }

        grant.Revoke(
            revokedAtUtc: timeProvider.GetUtcNow().UtcDateTime,
            revokedByUserId: revokedByUserId,
            revokedByUserName: revokedByUserName,
            reason: reason);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        // Write the revocation marker BEFORE returning so a racing request
        // doesn't slip through with a cached Active marker.
        await SetCachedStatusAsync(grant, GrantState.EndedOrRevoked, ct).ConfigureAwait(false);

        return ToDto(grant);
    }

    public  async Task<bool> IsRevokedOrEndedAsync(string jti, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jti)) return false;

        var state = await cache.GetOrCreateAsync(
            CacheKeys.ImpersonationGrantStatus(jti),
            new FactoryState(db, jti),
            LoadStateAsync,
            options: CacheEntryOptions,
            cancellationToken: ct).ConfigureAwait(false);

        return state == GrantState.EndedOrRevoked || state == GrantState.Unknown;
    }

    public async Task<IReadOnlyList<ImpersonationGrantDto>> ListAsync(ImpersonationGrantStatus? status, string? impersonatedTenantId, string? actorUserId, int take,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        IQueryable<ImpersonationGrant> q = db.ImpersonationGrants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(impersonatedTenantId))
        {
            q = q.Where(g => g.ImpersonatedTenantId == impersonatedTenantId);
        }
        if (!string.IsNullOrWhiteSpace(actorUserId))
        {
            q = q.Where(g => g.ActorUserId == actorUserId);
        }
        if (status is { } s)
        {
            q = s switch
            {
                ImpersonationGrantStatus.Active =>
                    q.Where(g => !g.RevokedAtUtc.HasValue && !g.EndedAtUtc.HasValue && g.ExpiresAtUtc > now),
                ImpersonationGrantStatus.Ended =>
                    q.Where(g => g.EndedAtUtc.HasValue),
                ImpersonationGrantStatus.Revoked =>
                    q.Where(g => g.RevokedAtUtc.HasValue),
                ImpersonationGrantStatus.Expired =>
                    q.Where(g => !g.RevokedAtUtc.HasValue && !g.EndedAtUtc.HasValue && g.ExpiresAtUtc <= now),
                _ => q,
            };
        }

        var rows = await q
            .OrderByDescending(g => g.StartedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(ToDto).ToList();
    }

    #region internals
    
    private readonly record struct FactoryState(IdentityDbContext db, string jti);

    private static readonly HybridCacheEntryOptions CacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
        Flags = HybridCacheEntryFlags.DisableCompression
    };
    
    private static async ValueTask<GrantState> LoadStateAsync(FactoryState s, CancellationToken ct)
    {
        var row = await s.db.ImpersonationGrants
            .AsNoTracking()
            .Where(g => g.Jit == s.jti)
            .Select(g => new { g.EndedAtUtc, g.RevokedAtUtc })
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (row is null) return GrantState.Unknown;
        if (row.EndedAtUtc.HasValue || row.RevokedAtUtc.HasValue) return GrantState.EndedOrRevoked;
        return GrantState.Active;
    }

    private Task SetCachedStatusAsync(ImpersonationGrant grant,GrantState state, CancellationToken ct = default)
        => cache.SetAsync(
            CacheKeys.ImpersonationGrantStatus(grant.Jit),
            state,
            options: CacheEntryOptions,
            cancellationToken: ct
            ).AsTask();
    
    private ImpersonationGrantDto ToDto(ImpersonationGrant g)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        ImpersonationGrantStatus status;
        if (g.RevokedAtUtc.HasValue)
        {
            status = ImpersonationGrantStatus.Revoked;
        }
        else if (g.EndedAtUtc.HasValue)
        {
            status = ImpersonationGrantStatus.Ended;
        }
        else if (g.ExpiresAtUtc <= now)
        {
            status = ImpersonationGrantStatus.Expired;
        }
        else
        {
            status = ImpersonationGrantStatus.Active;
        }

        return new ImpersonationGrantDto(
            Id: g.Id,
            Jti: g.Jit,
            ActorUserId: g.ActorUserId,
            ActorUserName: g.ActorUserName,
            ActorTenantId: g.ActorTenantId,
            ImpersonatedUserId: g.ImpersonatedUserId,
            ImpersonatedUserName: g.ImpersonatedUserName,
            ImpersonatedTenantId: g.ImpersonatedTenantId,
            Reason: g.Reason,
            StartedAtUtc: g.StartedAtUtc,
            ExpiresAtUtc: g.ExpiresAtUtc,
            EndedAtUtc: g.EndedAtUtc,
            RevokedAtUtc: g.RevokedAtUtc,
            RevokedByUserId: g.RevokedByUserId,
            RevokedByUserName: g.RevokedByUserName,
            RevokeReason: g.RevokeReason,
            Status: status);
    }

    #endregion
}