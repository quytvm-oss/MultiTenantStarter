using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.Extensions.Logging;

using Modules.Auditing.Contracts;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Impersonation.RevokeImpersonationGrant;

using Shared.Multitenancy;

namespace Modules.Identity.Features.v1.Impersonation.RevokeImpersonationGrant;

public class RevokeImpersonationGrantCommandHandler(
    IImpersonationGrantService grantService,
    ICurrentUser currentUser,
    ISecurityAudit securityAudit,
    IRequestContext requestContext,
    ILogger<RevokeImpersonationGrantCommandHandler> logger)
    : ICommandHandler<RevokeImpersonationGrantCommand, ImpersonationGrantDto>
{

    public async ValueTask<ImpersonationGrantDto> Handle(RevokeImpersonationGrantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }

        var callerUserId = currentUser.GetUserId().ToString();
        var callerTenantId = currentUser.GetTenantId()
            ?? throw new UnauthorizedException("missing tenant context");
        var isRoot = string.Equals(callerTenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal);

        // Enforce visibility before revoking: tenant admins may only revoke grants in their own
        // tenant. Cross-tenant grants return 404 (not 403) so existence isn't confirmed out of scope.
        var grant = await grantService.GetByIdAsync(command.GrantId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("impersonation grant not found");

        var withinTenant = string.Equals(grant.ImpersonatedTenantId, callerTenantId, StringComparison.Ordinal)
            || string.Equals(grant.ActorTenantId, callerTenantId, StringComparison.Ordinal);

        if (!isRoot && !withinTenant)
        {
            throw new NotFoundException("impersonation grant not found");
        }

        var updated = await grantService.RevokeAsync(
            id: command.GrantId,
            revokedByUserId: callerUserId,
            revokedByUserName: currentUser.Name,
            reason: command.Reason,
            ct: cancellationToken).ConfigureAwait(false);

        // Surface revoke as a first-class security event, queryable alongside Start/End entries.
        // The audit Reason is the revocation reason, not the original impersonation reason.
        await securityAudit.ImpersonationEndedAsync(
            actorUserId: grant.ActorUserId,
            actorTenantId: grant.ActorTenantId,
            targetUserId: grant.ImpersonatedUserId,
            targetTenantId: grant.ImpersonatedTenantId,
            clientId: requestContext.ClientId ?? "unknown",
            ct: cancellationToken).ConfigureAwait(false);

        logger.LogWarning(
            "Impersonation grant revoked: grantId={GrantId} jti={Jti} revokedBy={RevokedBy} reason={Reason}",
            updated.Id, updated.Jti, callerUserId, command.Reason ?? "<none>");

        return updated;
    }
}