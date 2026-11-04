using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Hosting;

using Rebus.ServiceProvider;

namespace Messaging.Rebus;

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
