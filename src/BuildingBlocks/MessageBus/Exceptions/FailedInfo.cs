using MessageBus.Constants;
using MessageBus.Model;

namespace MessageBus.Exceptions;

public class FailedInfo
{
    public IServiceProvider ServiceProvider { get; set; } = default!;
    
    public MessageType MessageType { get; set; }
    
    public Message Message { get; set; } = default!;
}