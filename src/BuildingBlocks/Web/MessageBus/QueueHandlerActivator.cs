using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Transport;

namespace Web.MessageBus;

public sealed class QueueHandlerActivator : IHandlerActivator
{
    private readonly IServiceProvider _rootProvider;
    private readonly IRebusHandlerRegistry _registry;
    private readonly string _queueName;

    public QueueHandlerActivator(IServiceProvider rootProvider, IRebusHandlerRegistry registry, string queueName)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));

        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        _queueName = !string.IsNullOrWhiteSpace(queueName) ? queueName : throw new ArgumentException("Queue name is required.", nameof(queueName));
    }

    public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
    {
        try
        {
            var scopedProvider = GetOrCreateScopeAndReturnServiceProvider(transactionContext);

            var descriptors = _registry.GetHandlers(_queueName, typeof(TMessage));

            var handlers = descriptors
                .Select(descriptor =>
                {
                    var instance = scopedProvider.GetRequiredService(descriptor.HandlerType);

                    if (instance is not IHandleMessages<TMessage> handler)
                    {
                        throw new InvalidOperationException(
                            $"Handler '{descriptor.HandlerType.FullName}' " +
                            $"cannot handle message '{typeof(TMessage).FullName}'. " +
                            $"Queue: '{_queueName}', " +
                            $"HandlerKey: '{descriptor.HandlerKey}'.");
                    }

                    return handler;
                })
                .ToArray();

            return Task.FromResult<IEnumerable<IHandleMessages<TMessage>>>(handlers);
        }
        catch (ObjectDisposedException exception)
        {
            throw new OperationCanceledException("Handler resolution aborted because the bus is shutting down.", exception);
        }
    }

    private IServiceProvider GetOrCreateScopeAndReturnServiceProvider(ITransactionContext transactionContext)
    {
        var stepContext = transactionContext.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);

        // Giống behavior của DependencyInjectionHandlerActivator:
        // gần như chỉ xảy ra trong test.
        if (stepContext == null)
        {
            return _rootProvider.CreateAsyncScope().ServiceProvider;
        }

        // Có scope do pipeline/user cung cấp trước rồi
        // thì reuse.
        var syncScope = stepContext.Load<IServiceScope>();

        if (syncScope != null)
        {
            return syncScope.ServiceProvider;
        }

        // Reuse AsyncServiceScope đã tồn tại.
        var asyncScope = stepContext.Load<AsyncServiceScope?>();

        if (asyncScope != null)
        {
            return asyncScope.Value.ServiceProvider;
        }

        // Chưa có -> tạo scope cho message hiện tại.
        var scope = _rootProvider.CreateAsyncScope();

        transactionContext.OnDisposed(_ =>
        {
            scope.DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();
        });

        stepContext.Save<AsyncServiceScope?>(scope);

        return scope.ServiceProvider;
    }
}

public sealed class QueueActivateHandlersStep : IIncomingStep
{
    private readonly ActivateHandlersStep _inner;
    private readonly string _queueName;

    public QueueActivateHandlersStep(IServiceProvider serviceProvider, IRebusHandlerRegistry registry, string queueName)
    {
        _queueName = queueName;

        var activator = new QueueHandlerActivator(serviceProvider, registry, queueName);

        _inner = new ActivateHandlersStep(activator);
    }

    public Task Process(IncomingStepContext context, Func<Task> next)
    {
        // Console.WriteLine(
        //     $"### QueueActivateHandlersStep RUN queue={_queueName}");

        return _inner.Process(context, next);
    }
}

