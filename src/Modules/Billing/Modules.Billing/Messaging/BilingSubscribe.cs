using System;
using System.Collections.Generic;
using System.Text;

using Messaging.Rebus;

using Modules.Multitenancy.Contracts.Events;

using Rebus.ServiceProvider;

namespace Modules.Billing.Messaging;

//public class BillingSubscribe : IRebusSubscription
//{
//    public async Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
//    {
//        var bus = busRegistry.GetBus(BillingConstant.QueueName);
//        await bus.Subscribe<TenantRenewedIntegrationEvent>();
//        await bus.Subscribe<TenantSubscribedIntegrationEvent>();
//    }
//}
