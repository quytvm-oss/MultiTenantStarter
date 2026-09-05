using Microsoft.Extensions.Hosting;

using Rebus.Bus;

namespace Web.MessageBus;

public sealed class RebusSubscriptionHostedService(IBus bus, IEnumerable<IRebusSubscription> subscriptions) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in subscriptions)
        {
            await subscription.SubscribeAsync(bus, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
