using System.Text.Json;
using MessageBus.Contracts;
using MessageBus.Model;
using Microsoft.Extensions.Options;

namespace MessageBus.Subscribes;

public sealed class SubscribeBusBuilder : ISubscribeBusBuilder
{
    private readonly List<ISubscriptionBuilder> _subscriptions = [];
    private readonly MessageBusOptions _messOptions;
    private readonly List<ConsumerExecutorRegistration> _registrations = [];
    private readonly Dictionary<string, List<ConsumerExecutorRegistration>> _routes = new();
    private readonly Dictionary<string, HashSet<string>> _groupRoutingKeys = [];

    private int _buildState = 0;

    
    private readonly Action<Type> _registerConsumer;
    private readonly JsonSerializerOptions _jsonOptions; 
    
    public SubscribeBusBuilder(Action<Type> registerConsumer, IOptions<MessageBusOptions> options)
    {
        _registerConsumer = registerConsumer;
        _messOptions = options.Value;
        _jsonOptions = options.Value.JsonSerializerOptions;
    }
    

    public IReadOnlyList<ConsumerExecutorRegistration> Registrations => _registrations.AsReadOnly();

    public MessageSubscriptionBuilder<TMessage> Subscribe<TMessage>()
    {
        ThrowIfBuilt();
        return new MessageSubscriptionBuilder<TMessage>(this);
    }

    public SubscriptionBuilder<TMessage, TConsumer> Subscribe<TMessage, TConsumer>()
        where TConsumer : class, IConsumer<TMessage>
    {
        ThrowIfBuilt();

        var builder = new SubscriptionBuilder<TMessage, TConsumer>(this);
        _subscriptions.Add(builder);
        return builder;
    }

    public ISubscribeBusBuilder Subscribe<TMessage, TConsumer>(Action<SubscriptionOptions> configure)
        where TConsumer : class, IConsumer<TMessage>
    {
        ArgumentNullException.ThrowIfNull(configure);
        ThrowIfBuilt();

        var options = new SubscriptionOptions();
        configure(options);
        Register<TMessage, TConsumer>(options);
        return this;
    }

    internal void Register<TMessage, TConsumer>(SubscriptionOptions options)
        where TConsumer : class, IConsumer<TMessage>
    {
        ThrowIfBuilt();

        var routingKey = NormalizeRoutingKey(options.Name);
        var group = !string.IsNullOrEmpty(options.Group) ? NormalizeGroup(options.Group) : _messOptions.DefaultGroupName;

        // if (_routes.TryGetValue(routingKey, out var existingRegistrations)
        //      && existingRegistrations.Any(x => x.Descriptor.MessageType != typeof(TMessage)))
        // {
        //      throw new InvalidOperationException(
        //         $"Routing key '{routingKey}' in group '{group}' is already bound to a different message type."); 
        // }

        var key = new SubscriptionKey(
            typeof(TMessage),
            typeof(TConsumer),
            group.ToUpperInvariant(),
            routingKey.ToUpperInvariant());

        // if (!_subscriptionKeys.Add(key))
        // {
        //     throw new InvalidOperationException(
        //         $"Consumer {typeof(TConsumer).FullName} is already subscribed to routing key '{routingKey}'.");
        // }

        var descriptor = new ConsumerDescriptor
        {
            MessageType = typeof(TMessage),
            ConsumerType = typeof(TConsumer),
            RoutingKey = !string.IsNullOrEmpty(_messOptions.TopicNamePrefix) ? $"{_messOptions.TopicNamePrefix}.{routingKey}" : routingKey,
            Group = !string.IsNullOrEmpty(_messOptions.GroupNamePrefix) ? $"{_messOptions.GroupNamePrefix}.{group}" :  group,
            GroupConcurrent = NormalizeGroupConcurrent(options.GroupConcurrent)
        };
        

        RegisterGroupRoutingKey(descriptor.Group, descriptor.RoutingKey);
        RegisterExecutor<TMessage, TConsumer>(descriptor.Group, descriptor.RoutingKey, descriptor);
        _registerConsumer(typeof(TConsumer));
    }

    public void Build()
    {
        if (Interlocked.CompareExchange(ref _buildState, 1, 0) != 0)
            return;

        foreach (var subscription in _subscriptions)
            subscription.Build();
    }

    private void RegisterGroupRoutingKey(string group, string routingKey)
    {
        if (!_groupRoutingKeys.TryGetValue(group, out var routingKeys))
        {
            routingKeys = [];
            _groupRoutingKeys[group] = routingKeys;
        }
        
        routingKeys.Add(routingKey);
    }

    private void RegisterExecutor<TMessage, TConsumer>(
        string group,
        string routingKey,
        ConsumerDescriptor descriptor)
        where TConsumer : class, IConsumer<TMessage>
    {
        var route = new SubscriptionRoute(group, routingKey);

        if (!_routes.TryGetValue(routingKey, out var registrations))
        {
            registrations = [];
            _routes[routingKey] = registrations;
        }

        var executor = new ConsumerExecutor<TMessage, TConsumer>();
        var registration = new ConsumerExecutorRegistration(descriptor, executor);

        registrations.Add(registration);
        _registrations.Add(registration);
    }

    internal void ThrowIfBuilt()
    {
        if (_buildState == 1)
            throw new InvalidOperationException(
                "Cannot add subscriptions after the subscribe bus has been built.");
    }

    private static string NormalizeRoutingKey(string routingKey)
    {
        if (string.IsNullOrWhiteSpace(routingKey))
            throw new InvalidOperationException("RoutingKey is required.");

        return routingKey.Trim();
    }

    private static string NormalizeGroup(string? group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return string.Empty;

        return group.Trim();
    }

    private static byte NormalizeGroupConcurrent(byte groupConcurrent)
    {
        return groupConcurrent == 0 ? (byte)1 : groupConcurrent;
    }
}

