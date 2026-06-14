using MessageBus.Constants;

namespace MessageBus.Model;

public class Message
{
    public object? Value { get; set; }
    
    public IDictionary<string, string?> Headers { get; set; }

    public Message()
    {
        Headers = new Dictionary<string, string?>();
    }
 
    public Message(IDictionary<string, string?> headers, object? value)
    {
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        Value   = value;
    }
}

public static class MessageExtensions
{
    public static string GetId(this Message message)
    {
        return message.Headers[HeaderConstant.MessageId]!;
    }
    
    public static string GetName(this Message message)
    {
        return message.Headers[HeaderConstant.MessageName]!;
    }
    
    public static string? GetGroup(this Message message)
    {
        message.Headers.TryGetValue(HeaderConstant.Group, out var value);
        return value;
    }
    
    public static int GetCorrelationSequence(this Message message)
    {
        if (message.Headers.TryGetValue(HeaderConstant.CorrelationSequence, out var value)) return int.Parse(value!);

        return 0;
    }
    
    public static string? GetExecutionInstanceId(this Message message)
    {
        message.Headers.TryGetValue(HeaderConstant.ExecutionInstanceId, out var value);
        return value;
    }
    
    public static bool HasException(this Message message)
    {
        return message.Headers.ContainsKey(HeaderConstant.Exception);
    }
    
    public static void AddOrUpdateException(this Message message, Exception ex)
    {
        var msg = $"{ex.GetType().Name}-->{ex.Message}";

        message.Headers[HeaderConstant.Exception] = msg;
    }
    
    public static void RemoveException(this Message message)
    {
        message.Headers.Remove(HeaderConstant.Exception);
    }
}