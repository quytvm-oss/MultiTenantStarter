using System;
using System.Collections.Generic;
using System.Text;

using Messaging.Rebus;

using Rebus.ServiceProvider;

namespace Modules.Multitenancy.Messaging;

internal class MultitenancySubscribe : IRebusSubscription
{
    public Task SubscribeAsync(IBusRegistry bus, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
