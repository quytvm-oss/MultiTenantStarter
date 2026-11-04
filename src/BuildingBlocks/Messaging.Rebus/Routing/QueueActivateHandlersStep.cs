using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Messaging.Rebus.Routing;

public sealed class QueueActivateHandlersStep : IIncomingStep
{
    private readonly ActivateHandlersStep _inner;

    public QueueActivateHandlersStep(IServiceProvider serviceProvider, IRebusHandlerRegistry registry, string queueName)
    {
        var activator = new QueueHandlerActivator(serviceProvider, registry, queueName);

        _inner = new ActivateHandlersStep(activator);
    }

    public Task Process(IncomingStepContext context, Func<Task> next)
    {
        return _inner.Process(context, next);
    }
}
