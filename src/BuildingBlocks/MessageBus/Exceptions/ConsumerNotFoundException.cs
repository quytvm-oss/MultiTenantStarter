namespace MessageBus.Exceptions;

public class ConsumerNotFoundException : Exception
{
    public ConsumerNotFoundException(string message) : base(message)
    {
    }
}