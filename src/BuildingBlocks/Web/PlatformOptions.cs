namespace Web;

public class PlatformOptions
{
    public bool EnableCors { get; set; } = true;
    public bool EnableOpenApi { get; set; } = true;
    public bool EnableCaching { get; set; } = false;
    public bool EnableJobs { get; set; } = false;
    public bool EnableMailing { get; set; } = false;
    public bool EnableOpenTelemetry { get; set; } = true;
    public bool EnableFeatureFlags { get; set; } = false;
    public bool EnableIdempotency { get; set; } = true;
    public bool EnableSse { get; set; } = false;
    public bool EnableRealtime { get; set; } = false;
    public bool EnableQuotas { get; set; } = false;
}