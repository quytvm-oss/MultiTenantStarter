using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Core.Context;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using MessageBus;

using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Events;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Tokens.TokenGeneration;

using Shared.Multitenancy;

namespace Modules.Identity.Features.v1.Tokens.TokenGeneration;

public class GenerateTokenCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    ISecurityAudit securityAudit,
    IRequestContext requestContext,
    IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor,
    ISessionService sessionService,
    IBusPublisher eventBus,
    ILogger<GenerateTokenCommandHandler> logger)
    : ICommandHandler<GenerateTokenCommand, TokenResponse>
{
    // private readonly IIdentityService _identityService = identityService;
    // private readonly ITokenService _tokenService = tokenService;
    // private readonly ISecurityAudit _securityAudit = securityAudit;
    // private readonly IRequestContext _requestContext = requestContext;
    // private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantContextAccessor = tenantContextAccessor;
    // private readonly ISessionService _sessionService = sessionService;
    // private readonly ILogger<GenerateTokenCommandHandler> _logger = logger;

    public async ValueTask<TokenResponse> Handle(GenerateTokenCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ip = requestContext.IpAddress ?? "unknown";
        var ua = requestContext.UserAgent ?? "unknown";
        var clientId = requestContext.ClientId ;

        var identityResult = await identityService
            .ValidateCredentialsAsync(command.Email, command.Password, command.TwoFactorCode, cancellationToken);

        if (identityResult is null)
        {
            await securityAudit.LoginFailedAsync(
                subjectIdOrName: command.Email,
                clientId: clientId!,
                reason: "Invalid credentials",
                ip: ip,
                ct: cancellationToken);
            
            throw new UnauthorizedAccessException("Invalid credentials.");
        }
        
        var (subject, claims) = identityResult.Value;

        await securityAudit.LoginSucceededAsync(
            userId: subject,
            userName: claims!.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? command.Email,
            clientId: clientId!,
            ip: ip,
            userAgent: ua,
            ct: cancellationToken);
        
        // Issue token
        var token = await tokenService.IssueAsync(subject, claims, null, cancellationToken);
        
        await identityService.StoreRefreshTokenAsync(subject, token.RefreshToken, token.RefreshTokenExpiresAt, cancellationToken);
        
        // Create user session for session management (non-blocking, fail gracefully)
        try
        {
            var refreshTokenHash = Sha256Short(token.RefreshToken);
            await sessionService.CreateSessionAsync(
                subject,
                refreshTokenHash, 
                ip,
                ua,
                token.RefreshTokenExpiresAt, 
                cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to create user session for user {UserId}. Login will continue without session tracking.", subject);
        }
        
        // 3) Audit token issuance with a fingerprint (never raw token)
        var fingerprint = Sha256Short(token.AccessToken);
        await securityAudit.TokenIssuedAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? command.Email,
            clientId: clientId!,
            tokenFingerprint: fingerprint,
            expiresUtc: token.AccessTokenExpiresAt,
            ct: cancellationToken);
        
        // 4) Enqueue integration event for token generation (sample event for testing eventing)
        var tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        var correlationId = Guid.CreateVersion7().ToString();
        
        var integrationEvent = new TokenGeneratedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
            TenantId: tenantId,
            UserId: subject,
            Email: command.Email,
            ClientId: clientId!,
            IpAddress: ip,
            UserAgent: ua,
            TokenFingerprint: fingerprint,
            AccessTokenExpiresAtUtc: token.AccessTokenExpiresAt);
        
        await eventBus.PublishAsync(integrationEvent,x =>
        {
            x.Name = "token.generated";
            x.Source = "Identity";
            x.TenantId = tenantId;
            x.CorrelationId = correlationId;
        }  , cancellationToken);
        
        return token;
    }

    private static string Sha256Short(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}