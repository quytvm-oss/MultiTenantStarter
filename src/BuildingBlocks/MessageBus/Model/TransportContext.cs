using MessageBus.Constants;

namespace MessageBus.Model;

public readonly struct TransportContext
{
    public ReadOnlyMemory<byte> Body { get; }
    
    public IDictionary<string, string?> Headers { get; }
    
    public TransportContext(IDictionary<string, string?> headers, ReadOnlyMemory<byte> body)
    {
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        Body = body;
    }
    
    
    public string GetId()
    {
        return Headers[HeaderConstant.MessageId]!;
    }
    
    public string GetName()
    {
        return Headers[HeaderConstant.MessageName]!;
    }

   
    public string? GetGroup()
    {
        return Headers.TryGetValue(HeaderConstant.Group, out var value) ? value : null;
    }

  
    public string? GetCorrelationId()
    {
        return Headers.TryGetValue(HeaderConstant.CorrelationId, out var value) ? value : null;
    }

   
    public string? GetExecutionInstanceId()
    {
        return Headers.TryGetValue(HeaderConstant.ExecutionInstanceId, out var value) ? value : null;
    }
}