using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Messaging.Rebus.Routing;

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
        Console.WriteLine(
            $"### QueueActivateHandlersStep RUN queue={_queueName}");

        return _inner.Process(context, next);
    }
}
