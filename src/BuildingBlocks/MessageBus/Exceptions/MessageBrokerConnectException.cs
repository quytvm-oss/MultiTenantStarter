namespace MessageBus.Exceptions;

public class MessageBrokerConnectException : Exception
{
    public MessageBrokerConnectException(Exception ex)
        : base("Broker Unreachable", ex)
    {
    }
}