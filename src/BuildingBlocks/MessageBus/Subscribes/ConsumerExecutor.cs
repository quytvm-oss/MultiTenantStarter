using MessageBus.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.Subscribes;

public sealed class ConsumerExecutor<TMessage, TConsumer> : IConsumerExecutor where TConsumer : class, IConsumer<TMessage>
{
    public async Task Execute(object message, IServiceProvider sp)
    {
        if (message is not TMessage typedMessage)
        {
            throw new InvalidOperationException(
                $"Message must be assignable to {typeof(TMessage).FullName}.");
        }

        var consumer = sp.GetRequiredService<TConsumer>();
        await consumer.Consume(typedMessage);
    }
}
