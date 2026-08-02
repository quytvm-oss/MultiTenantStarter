using System.IdentityModel.Tokens.Jwt;
using System.Net;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Impersonation.EndImpersonation;

using Shared.Identity;

namespace Modules.Identity.Features.v1.Impersonation.EndImpersonation;

public class EndImpersonationCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    ISecurityAudit securityAudit,
    ICurrentUser currentUser,
    IRequestContext requestContext,
    IImpersonationGrantService grantService,
    ILogger<EndImpersonationCommandHandler> logger)
    : ICommandHandler<EndImpersonationCommand, TokenResponse>
{

    public async ValueTask<TokenResponse> Handle(EndImpersonationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }
        
        var claims = currentUser.GetUserClaims()?.ToList()
            ?? throw new UnauthorizedException();

        var actorUserId = claims.FirstOrDefault(c => c.Type == ClaimConstants.ActorSubject)?.Value;
        var actorTenantId = claims.FirstOrDefault(c => c.Type == ClaimConstants.ActorTenant)?.Value;
        var jti = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrWhiteSpace(actorUserId) || string.IsNullOrWhiteSpace(actorTenantId))
        {
            throw new CustomException(
                "current session is not impersonation session",
                errors: null,
                HttpStatusCode.BadRequest);
        }

        var impersonatedUserId = currentUser.GetUserId().ToString();
        var impersonatedTenantId = currentUser.GetTenantId() ?? string.Empty;
        
        // Mark grant ended BEFORE issuing actor tokens so a racing JWT-hook request sees "ended" (safer than the reverse).
        // If MarkEnded fails we proceed anyway: the grant expires naturally and the hook treats Unknown states as revoked.
        if (!string.IsNullOrWhiteSpace(jti))
        {
            try
            {
                await grantService.MarkEndedByJtiAsync(jti, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogWarning(e,
                    "Failed to mark impersonation grant ended for jti={Jti}. Actor swap will still proceed.",
                    jti);
            }
        }

        var actorClaimsResult = await identityService
            .BuildClaimsForUserAsync(actorUserId, actorTenantId, cancellationToken);

        if (actorClaimsResult is null)
        {
            throw new NotFoundException("No claims were found for the current user");
        }
        
        var (subject, actorClaims) = actorClaimsResult.Value;
        
        var token = await tokenService.IssueAsync(subject, actorClaims, actorTenantId, cancellationToken);
        await identityService.StoreRefreshTokenAsync(subject, token.RefreshToken, token.RefreshTokenExpiresAt,
            cancellationToken);

        await securityAudit.ImpersonationEndedAsync(actorUserId: actorUserId,
            actorTenantId: actorTenantId,
            targetUserId: impersonatedUserId,
            targetTenantId: impersonatedTenantId,
            clientId: requestContext.ClientId ?? "unknown",
            ct: cancellationToken);
        
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Impersonation ended: actor {ActorUserId}@{ActorTenant} returned from {TargetUserId}@{TargetTenant} jti={Jti}",
                actorUserId, actorTenantId, impersonatedUserId, impersonatedTenantId, jti ?? "<missing>");
        }

        return token;
    }
}