using Core.Context;
using Core.Exceptions;

using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Impersonation.GetImpersonationGrants;
using Modules.Identity.Services;

using Shared.Multitenancy;

namespace Modules.Identity.Features.v1.Impersonation.GetImpersonationGrants;

public class GetImpersonationGrantsQueryHandler(
    IImpersonationGrantService impersonationGrantService,
    ICurrentUser currentUser)
    : IQueryHandler<GetImpersonationGrantsQuery, IReadOnlyList<ImpersonationGrantDto>>
{

    public async ValueTask<IReadOnlyList<ImpersonationGrantDto>> Handle(GetImpersonationGrantsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var callerTenant = currentUser.GetTenantId()
                           ?? throw new UnauthorizedException("missing tenant context");
        var isRoot = string.Equals(callerTenant, MultitenancyConstants.Root.Id, StringComparison.Ordinal);
        
        // Tenant scoping: root operators target any tenant; tenant admins are locked to their
        // own regardless of input. Mirrors the StartImpersonation cross-tenant rule.
        var tenantFilter = isRoot ? query.ImpersonatedTenantId : callerTenant;

        return await impersonationGrantService.ListAsync(
            status: query.Status,
            impersonatedTenantId:  tenantFilter,
            actorUserId: query.ActorUserId,
            take:  query.Take,
            ct: cancellationToken).ConfigureAwait(false);
    }
}