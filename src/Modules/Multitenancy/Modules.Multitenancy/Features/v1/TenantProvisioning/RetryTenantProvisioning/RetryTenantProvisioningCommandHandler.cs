using Mediator;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1.TenantProvisioning;
using Modules.Multitenancy.Provisioning;

namespace Modules.Multitenancy.Features.v1.TenantProvisioning.RetryTenantProvisioning;

public class RetryTenantProvisioningCommandHandler(ITenantProvisioningStarter provisioningStarter, ITenantProvisioningReader provisioningReader)
    : ICommandHandler<RetryTenantProvisioningCommand, TenantProvisioningStatusDto>
{

    public async ValueTask<TenantProvisioningStatusDto> Handle(RetryTenantProvisioningCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var correlationId = await provisioningStarter.RetryAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        var status = await provisioningReader.GetStatusAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        return status with { CorrelationId = correlationId };
    }
}