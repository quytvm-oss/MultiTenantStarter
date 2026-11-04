using Microsoft.Extensions.Logging;

using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Messaging.Rebus.Routing;

public sealed class QueueActivateHandlersStep : IIncomingStep
{
    private readonly ActivateHandlersStep _inner;

    private readonly ILogger<QueueActivateHandlersStep> _logger;
    private readonly string _queueName;

    public QueueActivateHandlersStep(IServiceProvider serviceProvider, IRebusHandlerRegistry registry, string queueName, ILogger<QueueActivateHandlersStep> logger)
    {
        _queueName = queueName;

        var activator = new QueueHandlerActivator(serviceProvider, registry, queueName);

        _inner = new ActivateHandlersStep(activator);
        _logger = logger;
    }

    public Task Process(IncomingStepContext context, Func<Task> next)
    {
        _logger.LogInformation(
            $"### QueueActivateHandlersStep RUN queue={_queueName}");

        return _inner.Process(context, next);
    }
}
