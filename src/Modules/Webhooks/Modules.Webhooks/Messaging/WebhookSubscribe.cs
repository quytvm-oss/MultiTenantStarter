using Rebus.Bus;
using Rebus.ServiceProvider;

using Shared.Webhooks;

using Web.MessageBus;

namespace Modules.Webhooks.Messaging;

public class WebhookSubscribe : IRebusSubscription
{
    public Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
    {
        var bus = busRegistry.GetBus("webhooks");
        bus.Subscribe<WebhookEvent>();
        return Task.CompletedTask;
    }

}
