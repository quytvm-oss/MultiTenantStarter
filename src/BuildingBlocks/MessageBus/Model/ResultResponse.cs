namespace MessageBus.Model;

public class ResultResponse
{
    public bool   Succeeded { get; private set; }
    public string? Error    { get; private set; }
    
    public Exception? Exception { get; set; }
 
    public static ResultResponse Success  => new() { Succeeded = true };
    
    public static ResultResponse Fail(string err) => new() { Error = err };
}