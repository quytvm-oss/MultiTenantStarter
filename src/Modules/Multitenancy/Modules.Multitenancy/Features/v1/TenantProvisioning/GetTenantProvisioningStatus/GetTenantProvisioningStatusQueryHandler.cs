using Mediator;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1.TenantProvisioning;
using Modules.Multitenancy.Provisioning;

namespace Modules.Multitenancy.Features.v1.TenantProvisioning.GetTenantProvisioningStatus;

public class GetTenantProvisioningStatusQueryHandler(ITenantProvisioningReader tenantThemeService)
    : IQueryHandler<GetTenantProvisioningStatusQuery, TenantProvisioningStatusDto>
{
    public async ValueTask<TenantProvisioningStatusDto> Handle(GetTenantProvisioningStatusQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await tenantThemeService.GetStatusAsync(query.TenantId, cancellationToken)
            .ConfigureAwait(false);
    }
}