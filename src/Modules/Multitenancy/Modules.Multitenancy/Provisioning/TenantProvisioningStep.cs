using System.ComponentModel.DataAnnotations.Schema;

namespace Modules.Multitenancy.Provisioning;

public class TenantProvisioningStep
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ProvisioningId { get; private set; }

    public TenantProvisioningStepName Step { get; private set; }

    public TenantProvisioningStatus Status { get; private set; } = TenantProvisioningStatus.Pending;
    
    public string? Error { get; private set; }

    public DateTime? StartedUtc { get; private set; }

    public DateTime? CompletedUtc { get; private set; }

    [ForeignKey(nameof(ProvisioningId))]
    public TenantProvisioning? Provisioning { get; private set; }
    
    private TenantProvisioningStep() {}
    
    
}