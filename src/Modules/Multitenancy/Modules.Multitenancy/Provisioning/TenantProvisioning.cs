namespace Modules.Multitenancy.Provisioning;

public class TenantProvisioning
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string TenantId { get; set; } = default!;

    public string CorrelationId { get; set; } = default!;

    public TenantProvisioningStatus Status { get; private set; } = TenantProvisioningStatus.Pending;

    public string? CurrentStep { get; private set; }

    public string? Error { get; private set; }

    public string? JobId { get; private set; }

    public DateTime CreatedUtc { get; private set; } = TimeProvider.System.GetUtcNow().UtcDateTime;

    public DateTime? StartedUtc { get; private set; }

    public DateTime? CompletedUtc { get; private set; }

    public ICollection<TenantProvisioningStep> Steps { get; private set; } = new List<TenantProvisioningStep>();
    
    private TenantProvisioning() {}
    
    
}