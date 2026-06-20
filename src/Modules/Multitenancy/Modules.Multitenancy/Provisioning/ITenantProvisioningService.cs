using Modules.Multitenancy.Contracts.Dtos;

namespace Modules.Multitenancy.Provisioning;

public interface ITenantProvisioningService
{
    Task<TenantProvisioning> StartAsync(string tenantId, CancellationToken cancellationToken);
    
    Task<TenantProvisioning?> GetLastestAsync(string tenantId, CancellationToken cancellationToken);
    
    Task<TenantProvisioningStatusDto> GetStatusAsync(string tenantId, CancellationToken cancellationToken);
    
    Task EnsureCanActivateAsync(string tenantId, CancellationToken cancellationToken);
    
    Task<string> RetryAsync(string tenantId, CancellationToken cancellationToken);

    Task<bool> MarkRunningAsync(string tenantId, string correlationId,TenantProvisioningStepName step, CancellationToken cancellationToken);
    
    Task MarkStepCompletedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, CancellationToken cancellationToken);
    
    Task MarkStepFailedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, string? error, CancellationToken cancellationToken);
    
    Task MarkCompletedAsync(string tenantId, string correlationId, CancellationToken cancellationToken);
}

public interface ITenantProvisioningStarter
{
    Task<TenantProvisioning> StartAsync(string tenantId, CancellationToken cancellationToken);

    Task<string> RetryAsync(string tenantId, CancellationToken cancellationToken);
}

public interface ITenantProvisioningReader
{
    Task<TenantProvisioning?> GetLastestAsync(string tenantId, CancellationToken cancellationToken);

    Task<TenantProvisioningStatusDto> GetStatusAsync(string tenantId, CancellationToken cancellationToken);

    Task EnsureCanActivateAsync(string tenantId, CancellationToken cancellationToken);
}

public interface ITenantProvisioningStateWriter
{
    Task<bool> MarkRunningAsync(
        string tenantId,
        string correlationId,
        TenantProvisioningStepName step,
        CancellationToken cancellationToken);

    Task MarkStepCompletedAsync(
        string tenantId,
        string correlationId,
        TenantProvisioningStepName step,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        string tenantId,
        string correlationId,
        TenantProvisioningStepName step,
        string? error,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        string tenantId,
        string correlationId,
        CancellationToken cancellationToken);
}