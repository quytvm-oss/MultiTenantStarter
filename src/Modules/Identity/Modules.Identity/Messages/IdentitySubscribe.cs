using Rebus.ServiceProvider;

using Shared.Webhooks;

using Web.MessageBus;

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

