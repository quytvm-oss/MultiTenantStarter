using MessageBus.Contracts;
using MessageBus.Model;

namespace MessageBus.Subscribes;

public sealed class SubscriptionBuilder<TMessage, TConsumer> : ISubscriptionBuilder
    where TConsumer : class, IConsumer<TMessage>
{
    private readonly SubscribeBusBuilder _builder;
    private readonly SubscriptionOptions _options = new();

    internal SubscriptionBuilder(SubscribeBusBuilder builder)
    {
        _builder = builder;
    }

    public SubscriptionBuilder<TMessage, TConsumer> WithName(string routingKey)
    {
        _builder.ThrowIfBuilt();
        _options.Name = routingKey;
        return this;
    }

    public SubscriptionBuilder<TMessage, TConsumer> WithGroup(string group)
    {
        _builder.ThrowIfBuilt();
        _options.Group = group;
        return this;
    }

    void ISubscriptionBuilder.Build()
    {
        if (string.IsNullOrWhiteSpace(_options.Name))
            throw new InvalidOperationException(
                $"RoutingKey is required for consumer '{typeof(TConsumer).FullName}'.");

        _builder.Register<TMessage, TConsumer>(_options);
    }
}