using System.Text.Json;
using MessageBus.Contracts;
using MessageBus.Model;
using Microsoft.Extensions.Options;

namespace MessageBus.Processors;

// internal class Serializer : ISerializer
// {
//     private readonly JsonSerializerOptions _jsonSerializerOptions;
//
//     public Serializer(IOptions<MessageBusOptions> capOptions)
//     {
//         _jsonSerializerOptions = capOptions.Value.JsonSerializerOptions;
//     }
//
//     public ValueTask<TransportContext> SerializeAsync(Message message)
//     {
//         if (message == null) throw new ArgumentNullException(nameof(message));
//
//         if (message.Value == null) return new ValueTask<TransportContext>(new TransportContext(message.Headers, null));
//
//         var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message.Value, _jsonSerializerOptions);
//
//         return new ValueTask<TransportContext>(new TransportContext(message.Headers, jsonBytes));
//     }
//
//     public ValueTask<Message> DeserializeAsync(TransportContext transportMessage, Type? valueType)
//     {
//         if (valueType == null || transportMessage.Body.Length == 0)
//             return new ValueTask<Message>(new Message(transportMessage.Headers, null));
//
//         var obj = JsonSerializer.Deserialize(transportMessage.Body.Span, valueType, _jsonSerializerOptions);
//
//         return new ValueTask<Message>(new Message(transportMessage.Headers, obj));
//     }
//
//     public string Serialize(Message message)
//     {
//         return JsonSerializer.Serialize(message, _jsonSerializerOptions);
//     }
//
//     public Message? Deserialize(string json)
//     {
//         return JsonSerializer.Deserialize<Message>(json, _jsonSerializerOptions);
//     }
//
//     public object? Deserialize(object value, Type valueType)
//     {
//         return value switch
//         {
//             JsonElement jsonElement => jsonElement.Deserialize(valueType, _jsonSerializerOptions),
//             string json => JsonSerializer.Deserialize(json, valueType, _jsonSerializerOptions),
//             _ when valueType.IsInstanceOfType(value) => value,
//             _ => throw new NotSupportedException(
//                 $"Cannot deserialize value of type '{value.GetType().FullName}' to '{valueType.FullName}'.")
//         };
//     }
//
//     public bool IsJsonType(object jsonObject)
//     {
//         return jsonObject is JsonElement;
//     }
// }

internal class Serializer : ISerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public Serializer(IOptions<MessageBusOptions> options)
    {
        _jsonSerializerOptions = options.Value.JsonSerializerOptions;
    }

     public ValueTask<TransportContext> SerializeAsync(Message message)
     {
         if (message == null) throw new ArgumentNullException(nameof(message));

         if (message.Value == null) return new ValueTask<TransportContext>(new TransportContext(message.Headers, null));

         var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message.Value, _jsonSerializerOptions);

         return new ValueTask<TransportContext>(new TransportContext(message.Headers, jsonBytes));
     }

     public ValueTask<Message> DeserializeAsync(TransportContext transportMessage, Type? valueType)
     {
         if (valueType == null || transportMessage.Body.Length == 0)
             return new ValueTask<Message>(new Message(transportMessage.Headers, null));

         var obj = JsonSerializer.Deserialize(transportMessage.Body.Span, valueType, _jsonSerializerOptions);

         return new ValueTask<Message>(new Message(transportMessage.Headers, obj));
     }
    
    public object? DeserializeContent(string content, Type valueType)
    {
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement
            .GetProperty("Value")
            .Deserialize(valueType, _jsonSerializerOptions);
    }

    public string Serialize(Message message)
    {
        return JsonSerializer.Serialize(message, _jsonSerializerOptions);
    }

    public Message? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<Message>(json, _jsonSerializerOptions);
    }
}