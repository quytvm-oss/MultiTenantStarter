using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Core.Context;

using Finbuckle.MultiTenant.Abstractions;

using MassTransit;

using Mediator;

using Messaging.Persistence;

using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Events;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using Modules.Webhooks.Contracts.Events;

using Shared.Multitenancy;

namespace Modules.Identity.Features.v1.Tokens.TokenGeneration;

public class GenerateTokenCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    ISecurityAudit securityAudit,
    IRequestContext requestContext,
    IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor,
    ISessionService sessionService,
    IPublishEndpoint publishEndpoint,
    MessagingDbContext messagingDb,
    ILogger<GenerateTokenCommandHandler> logger)
    : ICommandHandler<GenerateTokenCommand, TokenResponse>
{

    public async ValueTask<TokenResponse> Handle(GenerateTokenCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ip = requestContext.IpAddress ?? "unknown";
        var ua = requestContext.UserAgent ?? "unknown";
        var clientId = requestContext.ClientId;

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
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
            TenantId: tenantId,
            Source: "Identity",
            CorrelationId: correlationId,
            UserId: subject,
            Email: command.Email,
            ClientId: clientId!,
            IpAddress: ip,
            UserAgent: ua,
            TokenFingerprint: fingerprint,
            AccessTokenExpiresAtUtc: token.AccessTokenExpiresAt);

        var webhookEvent = new WebhookEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
            TenantId: tenantId,
            CorrelationId: correlationId,
            Source: "Identity",
            EventType: "identity.token.generated",
            FullName: typeof(TokenGeneratedIntegrationEvent).FullName!,
            Payload: JsonSerializer.Serialize(integrationEvent),
            UserId: subject);

        await publishEndpoint.Publish(webhookEvent, context =>
        {
            context.Delay = TimeSpan.FromMinutes(2);
        });
        await messagingDb.SaveChangesAsync(cancellationToken);
        return token;
    }

    private static string Sha256Short(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}