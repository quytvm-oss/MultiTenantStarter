namespace MessageBus.Model;

public sealed class SubscriptionOptions
{
    public string Name { get; set; } = string.Empty;
    public string? Group { get; set; }
    public byte GroupConcurrent { get; set; } = 1;
}