using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

using Shared.Identity;
using Shared.Multitenancy;

namespace Modules.Identity.Features.v1.Impersonation.StartImpersonation;

public class StartImpersonationCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    ISecurityAudit securityAudit,
    ICurrentUser currentUser,
    IRequestContext requestContext,
    IImpersonationGrantService grantService,
    TimeProvider timeProvider,
    ILogger<StartImpersonationCommandHandler> logger)
    : ICommandHandler<StartImpersonationCommand, ImpersonationResponse>
{

    public async ValueTask<ImpersonationResponse> Handle(StartImpersonationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }
        
        var actorUserId = currentUser.GetUserId().ToString();
        var actorTenantId = currentUser.GetTenantId() ??
            throw new UnauthorizedException("missing tenant context.");

        var actorUserName = currentUser.Name;
        
        // Cross-tenant impersonation requires the actor to be in the root tenant. Tenant admins
        // can only impersonate users within their own tenant.
        if (!string.Equals(actorTenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal)
            && !string.Equals(actorTenantId, command.TargetTenantId, StringComparison.Ordinal))
        {
            throw new CustomException("cannot impersonate yourself", errors: null, HttpStatusCode.BadRequest);
        }
        
        // Prevent self-impersonation (pointless, confuses the audit trail). Caller error → explicit 4xx,
        // not the 500 CustomException defaults to.
        if (string.Equals(actorUserId, command.TargetUserId, StringComparison.Ordinal)
            && string.Equals(actorTenantId, command.TargetTenantId, StringComparison.Ordinal))
        {
            throw new CustomException("cannot impersonate yourself", errors: null, System.Net.HttpStatusCode.BadRequest);
        }
        
        // Prevent nesting: if the caller is already impersonating, require end-impersonation first.
        var callerClaims = currentUser.GetUserClaims();
        if (callerClaims is not null && callerClaims.Any(c => c.Type == ClaimConstants.ActorSubject))
        {
            throw new CustomException(
                "end current impersonation before starting a new one",
                errors: null, 
                HttpStatusCode.BadRequest);
        }
        
        var targetClaimsResult = await identityService
            .BuildClaimsForUserAsync(command.TargetUserId, command.TargetTenantId, cancellationToken);
        
        if (targetClaimsResult is null)
        {
            throw new NotFoundException("target user not found");
        }
        
        var (subject, claims) = targetClaimsResult.Value;
        var targetUserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
        
        // Strip the auto-generated jti from BuildClaimsForUserAsync and inject our own, so the persisted
        // ImpersonationGrant row and the issued JWT share the same jti.
        var jti = Guid.CreateVersion7().ToString("N");
        var impersonationClaims = claims
            .Where(c => c.Type != JwtRegisteredClaimNames.Jti)
            .Concat([
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                // RFC 8693 actor claims so the issued token carries who is acting.
                new Claim(ClaimConstants.ActorSubject, actorUserId),
                new Claim(ClaimConstants.ActorTenant, actorTenantId)
            ]).ToList();
        
        // Cap the caller-supplied duration server-side (defense in depth: the validator already rejects
        // out-of-range, but a future caller bypassing it must not escape the cap).
        var lifetime = command.DurationMinutes is { } minutes
            ? TimeSpan.FromMinutes(Math.Clamp(minutes, 1,
                StartImpersonationCommandValidator.MaxImpersonationMinutes))
            : (TimeSpan?)null;

        var startedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var (accessToken, expiresAt) = await tokenService.IssueAccessOnlyAsync(
            subject, impersonationClaims, lifetime, cancellationToken);
        
        // Persist the grant AFTER issuance so a failed issue leaves no orphan grant. CreateAsync primes the
        // cache so the JWT validation hook sees status=Active on the next request without a DB hit.
        await grantService.CreateAsync(new CreateGrantInput(
            Jti: jti,
            ActorUserId: actorUserId,
            ActorUserName: actorUserName,
            ActorTenantId: actorTenantId,
            ImpersonatedUserId: subject,
            ImpersonatedUserName: targetUserName,
            ImpersonatedTenantId: command.TargetTenantId,
            Reason: command.Reason ?? string.Empty,
            StartedAtUtc: startedAtUtc,
            ExpiresAtUtc: expiresAt,
            ClientId: requestContext.ClientId,
            IpAddress: requestContext.IpAddress,
            UserAgent: requestContext.UserAgent), cancellationToken);
        
        await securityAudit.ImpersonationStartedAsync(
            actorUserId: actorUserId,
            actorTenantId: actorTenantId,
            targetUserId: subject,
            targetTenantId: command.TargetTenantId,
            clientId: requestContext.ClientId ?? "unknown",
            ip: requestContext.IpAddress ?? "unknown",
            userAgent: requestContext.UserAgent ?? "unknown",
            reason: command.Reason ?? string.Empty,
            ct: cancellationToken);

        logger.LogWarning(
            "Impersonation started: actor {ActorUserId}@{ActorTenant} -> target {TargetUserId}@{TargetTenant} jti={Jti}",
            actorUserId, actorTenantId, subject, command.TargetTenantId, jti);

        return new ImpersonationResponse(
            AccessToken: accessToken,
            AccessTokenExpiresAt: expiresAt,
            ActorUserId: actorUserId,
            ActorTenantId: actorTenantId,
            ImpersonatedUserId: subject,
            ImpersonatedTenantId: command.TargetTenantId);
    }
}