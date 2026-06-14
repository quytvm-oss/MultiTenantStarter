using MessageBus.Constants;

namespace MessageBus.Model;

public class MessageContext
{
    public string DbId { get; set; } = default!;
    
    public string Name  { get; set; } = default!;
    
    public string Group  { get; set; } = default!;

    public Message Origin { get; set; } = default!;

    public string Content { get; set; } = default!;

    public DateTime Added { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int Retries { get; set; }
    
    public StatusName StatusName { get; set; }
}