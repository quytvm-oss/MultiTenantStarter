using Microsoft.Extensions.Hosting;

using Rebus.Bus;
using Rebus.ServiceProvider;

namespace Web.MessageBus;

public sealed class RebusSubscriptionHostedService(IBusRegistry busRegistry, IEnumerable<IRebusSubscription> subscriptions) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptions)
        {
            await subscription.SubscribeAsync(busRegistry, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
