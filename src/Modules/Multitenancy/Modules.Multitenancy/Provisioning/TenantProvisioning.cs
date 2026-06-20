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

    public TenantProvisioning(string tenantId, string correlationId)
    {
        TenantId = tenantId;
        CorrelationId = correlationId;
        CreatedUtc = TimeProvider.System.GetUtcNow().UtcDateTime;
    }
    
    public void SetJobId(string jobId) => JobId = jobId;

    public void MarkRunning(string step)
    {
        Status = TenantProvisioningStatus.Running;
        StartedUtc ??= TimeProvider.System.GetUtcNow().UtcDateTime;
        CurrentStep = step;
    }

    public void MarkCompleted()
    {
        Status = TenantProvisioningStatus.Completed;
        CompletedUtc ??= TimeProvider.System.GetUtcNow().UtcDateTime;
        CurrentStep = null;
        Error = null;
    }

    public void MarkFailed(string error, string step)
    {
        Status = TenantProvisioningStatus.Failed;
        CompletedUtc ??= TimeProvider.System.GetUtcNow().UtcDateTime;
        CurrentStep = step;
        Error = error;
    }
}