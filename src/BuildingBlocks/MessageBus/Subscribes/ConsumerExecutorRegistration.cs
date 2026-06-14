using MessageBus.Contracts;
using MessageBus.Model;

namespace MessageBus.Subscribes;


public sealed record ConsumerExecutorRegistration(ConsumerDescriptor Descriptor, IConsumerExecutor Executor);

public readonly record struct SubscriptionRoute(
    string Group,
    string RoutingKey);
    
public readonly record struct SubscriptionKey(
    Type MessageType,
    Type ConsumerType,
    string Group,
    string RoutingKey);