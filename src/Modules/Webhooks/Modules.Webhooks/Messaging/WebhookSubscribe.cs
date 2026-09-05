using Rebus.Bus;

using Shared.Webhooks;

using Web.MessageBus;

namespace Modules.Webhooks.Messaging;

public class WebhookSubscribe : IRebusSubscription
{
    public Task SubscribeAsync(IBus bus, CancellationToken cancellationToken)
    {
        bus.Subscribe<WebhookEvent>();
        return Task.CompletedTask;
    }

}
