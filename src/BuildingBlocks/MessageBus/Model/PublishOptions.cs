namespace MessageBus.Model;

public sealed class PublishOptions
{
    public string? Name { get; set; }

    public TimeSpan? Delay { get; set; }
    
    public string? TenantId { get; set; }
    
    public string? Source { get; set; }
    
    public string? CorrelationId { get; set; }

    public Dictionary<string, string> Header { get; set; } = new();
}