namespace MessageBus.Model;

public class LogMessageEventArgs : EventArgs
{
    public string? Reason { get; set; }
}