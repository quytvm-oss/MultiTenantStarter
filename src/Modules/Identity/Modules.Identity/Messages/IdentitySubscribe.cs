using Rebus.ServiceProvider;

using Shared.Webhooks;

using Messaging.Rebus;

namespace Modules.Identity.Messages;

public class IdentitySubscribe : IRebusSubscription
{
    public async Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
    {
        var bus = busRegistry.GetBus("identity");
        await bus.Subscribe<WebhookEvent>();
    }

}

