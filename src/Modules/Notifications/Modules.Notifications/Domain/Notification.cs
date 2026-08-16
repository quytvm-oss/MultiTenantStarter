using Core.Domain;

namespace Modules.Notifications.Domain;

public class Notification : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = default!;
    
    /// <summary>Logical event type, e.g. <c>chat.mention</c>. Used by the UI to pick an icon.</summary>
    public string Type { get; private set; } = default!;

    public string Title { get; private set; } = default!;

    public Platform Platform { get; set; }
    
    public string? Body { get; private set; }
    
    public string? Link { get; private set; }
    
    /// <summary>Originating module name (e.g. <c>Chat</c>) — for grouping + filtering.</summary>
    public string Source { get; private set; } = default!;

    /// <summary>Opaque JSON blob — the source module owns the shape.</summary>
    public string MetadataJson { get; private set; } = "{}";

    public DateTime? ReadAtUtc { get; private set; }
    
    public DateTime CreatedAtUtc { get; private set; }
    
    
}

public enum Type
{
}

public enum Platform
{
    Web,
    
    Mobile
}