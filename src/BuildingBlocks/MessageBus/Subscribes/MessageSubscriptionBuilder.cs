using MessageBus.Contracts;
using MessageBus.Model;

namespace MessageBus.Subscribes;

public sealed class MessageSubscriptionBuilder<TMessage>
{
    private readonly SubscribeBusBuilder _builder;
    private readonly SubscriptionOptions _options = new();

    internal MessageSubscriptionBuilder(SubscribeBusBuilder builder)
    {
        _builder = builder;
    }

    public MessageSubscriptionBuilder<TMessage> WithName(string name)
    {
        _builder.ThrowIfBuilt();
        _options.Name = name;
        return this;
    }

    public MessageSubscriptionBuilder<TMessage> WithGroup(string group)
    {
        _builder.ThrowIfBuilt();
        _options.Group = group;
        return this;
    }

    public MessageSubscriptionBuilder<TMessage> Consumer<TConsumer>()
        where TConsumer : class, IConsumer<TMessage>
    {
        _builder.Register<TMessage, TConsumer>(_options);
        return this;
    }
}
