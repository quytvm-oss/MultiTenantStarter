using MessageBus.Model;
using MessageBus.Subscribes;

namespace MessageBus.Contracts;

public interface ISubscribeBusBuilder
{
    MessageSubscriptionBuilder<TMessage> Subscribe<TMessage>();

    SubscriptionBuilder<TMessage, TConsumer> Subscribe<TMessage, TConsumer>()
        where TConsumer : class, IConsumer<TMessage>;

    ISubscribeBusBuilder Subscribe<TMessage, TConsumer>(
        Action<SubscriptionOptions> configure)
        where TConsumer : class, IConsumer<TMessage>;

    IReadOnlyList<ConsumerExecutorRegistration> Registrations { get; }

    void Build();
}