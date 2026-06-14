namespace Web.RateLimiting;

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public FixedWindowPolicyOptions User { get; set; } = new() { PermitLimit = 200, WindowSeconds = 60, QueueLimit = 0 };
    public FixedWindowPolicyOptions Ip { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60, QueueLimit = 0 };
    public FixedWindowPolicyOptions Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60, QueueLimit = 0 };

    public Dictionary<string, FixedWindowPolicyOptions> TenantPlans { get; set; } = new()
    {
        ["free"]       = new() { PermitLimit = 100,  WindowSeconds = 60, QueueLimit = 0 },
        ["pro"]        = new() { PermitLimit = 500,  WindowSeconds = 60, QueueLimit = 0 },
        ["enterprise"] = new() { PermitLimit = 2000, WindowSeconds = 60, QueueLimit = 0 }
    };

    public FixedWindowPolicyOptions GetPlanPolicy(string? plan) =>
        TenantPlans.TryGetValue(plan ?? "free", out var policy) ? policy : TenantPlans["free"];
}