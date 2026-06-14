using MessageBus.Model;

namespace MessageBus.Contracts;

public interface ISerializer
{
    string Serialize(Message message);

    ValueTask<TransportContext> SerializeAsync(Message message);
    
    Message? Deserialize(string json);

 
    ValueTask<Message> DeserializeAsync(TransportContext transportMessage, Type? valueType);
    
    object? DeserializeContent(string content, Type valueType);
}