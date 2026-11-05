using System;
using System.Collections.Generic;
using System.Text;

using Messaging.Rebus;

using Rebus.ServiceProvider;

namespace Modules.Multitenancy.Messaging;

internal class MultitenancySubscribe : IRebusSubscription
{
    public async Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
    {
        var bus = busRegistry.GetBus(MultitenancyModuleConstant.QueueName);
        // Subscribe to integration events
       
    }
}
