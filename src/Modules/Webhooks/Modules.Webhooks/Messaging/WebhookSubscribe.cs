using Messaging.Rebus;

using Rebus.ServiceProvider;

using Shared.Webhooks;

namespace Modules.Webhooks.Messaging;

public class WebhookSubscribe : IRebusSubscription
{
    public Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
    {
        var bus = busRegistry.GetBus(WebhooksModuleConstant.QueueName);
        bus.Unsubscribe<WebhookEvent>();
        return Task.CompletedTask;
    }

}
