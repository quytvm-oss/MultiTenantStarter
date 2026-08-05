using Mediator;

using Modules.Multitenancy.Contracts.Dtos;

namespace Modules.Multitenancy.Contracts.v1.TenantProvisioning;

public record RetryTenantProvisioningCommand(string TenantId) : ICommand<TenantProvisioningStatusDto>;