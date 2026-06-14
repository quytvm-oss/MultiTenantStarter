namespace MessageBus;

public interface IConsumer<in TMessage>
{
    Task Consume(TMessage message);
}