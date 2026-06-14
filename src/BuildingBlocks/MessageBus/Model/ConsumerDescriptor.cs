using System.Text.Json.Serialization.Metadata;

namespace MessageBus.Model;

public sealed record ConsumerDescriptor
{
    public required Type MessageType { get; init; }
    //public required JsonTypeInfo MessageTypeInfo { get; init; }
    public required Type ConsumerType { get; init; }
    public required string RoutingKey { get; init; }
    public string? Group { get; init; }
    public byte GroupConcurrent { get; set; } = 1;
}