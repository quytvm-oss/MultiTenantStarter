using System.Security.Claims;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using Modules.Identity.Helpers;

namespace Modules.Identity.Features.v1.Tokens.RefreshToken;

public class RefreshTokenCommandHandler(
    ITokenService tokenService,
    IIdentityService identityService,
    ISecurityAudit securityAudit,
    IRequestContext requestContext,
    ILogger<RefreshTokenCommandHandler> logger)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenCommandResponse>
{

    public async ValueTask<RefreshTokenCommandResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var clientId = requestContext.ClientId;
        
        var validated = await identityService.ValidateRefreshTokenAsync(command.RefreshToken, cancellationToken);

        if (validated is null)
        {
            await securityAudit.TokenRevokedAsync("unknown", clientId!, "InvalidRefreshToken", cancellationToken);
            throw new UnauthorizedException("Invalid refresh token.");
        }
        
        var (subject, claims) = validated.Value;
        
        // Audit previous token revocation by rotation (no raw tokens)
        await securityAudit.TokenRevokedAsync(subject, clientId!, "RefreshTokenRotated", cancellationToken);
        
        // Issue new tokens
        IEnumerable<Claim> enumerable = claims.ToList();
        var newToken = await tokenService.IssueAsync(subject, enumerable, null, cancellationToken);
        
        // Persist rotated refresh token for this user session
        await identityService.RotateRefreshTokenAsync(command.RefreshToken, newToken.RefreshToken, newToken.RefreshTokenExpiresAt, cancellationToken);

        // Audit the newly issued token with a fingerprint
        var fingerprint = TokenHasher.Fingerprint(newToken.AccessToken);
        await securityAudit.TokenIssuedAsync(
            userId: subject,
            userName: enumerable.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty,
            clientId: clientId!,
            tokenFingerprint: fingerprint,
            expiresUtc: newToken.AccessTokenExpiresAt,
            ct: cancellationToken);
        
        return new RefreshTokenCommandResponse(
            AccessToken: newToken.AccessToken,
            RefreshToken: newToken.RefreshToken,
            RefreshTokenExpiresAt: newToken.RefreshTokenExpiresAt);
    }
}