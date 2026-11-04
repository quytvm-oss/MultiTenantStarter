using Messaging.Rebus;

using Rebus.ServiceProvider;

using Shared.Webhooks;

namespace Modules.Identity.Messages;

public class IdentitySubscribe : IRebusSubscription
{
    public Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
    {
        var bus = busRegistry.GetBus("identity");
        bus.Subscribe<WebhookEvent>();
        return Task.CompletedTask;
    }

}

