using Messaging.Rebus;

using Rebus.ServiceProvider;

using Shared.Webhooks;

namespace Modules.Identity.Messages;

public class IdentitySubscribe : IRebusSubscription
{
    public async Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
    {
        var bus = busRegistry.GetBus(IdentityModuleConstants.QueueName);
        await bus.Subscribe<WebhookEvent>();
    }

}

