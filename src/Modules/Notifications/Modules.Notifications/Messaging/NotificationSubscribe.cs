using System;
using System.Collections.Generic;
using System.Text;

using Messaging.Rebus;

using Modules.Billing.Contracts.Events;

using Rebus.ServiceProvider;

namespace Modules.Notifications.Messaging;

//public class NotificationSubscribe : IRebusSubscription
//{
//    public async Task SubscribeAsync(IBusRegistry busRegistry, CancellationToken cancellationToken)
//    {
//        var bus = busRegistry.GetBus(NotificationModuleConstant.QueueName);
//        await bus.Subscribe<InvoiceIssuedIntegrationEvent>();
//    }
//}
