using System;
using System.Collections.Generic;
using System.Text;

using Messaging.Rebus.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.Billing.Events;
using Modules.Identity.Events;

namespace Modules.Billing.Messaging;

internal static class BillingMessagingRegistration
{

    //public static IServiceCollection AddBillingMessaging(this IServiceCollection services, IConfiguration configuration)
    //{
    //    services.AddHeroMessaging(configuration, moduleKey: BillingConstant.QueueName, isPrimary: true);
    //    services.AddModuleBus<BillingModule>(BillingConstant.QueueName);

    //    //register handler
    //    services.AddQueueHandler<TenantRenewedIntegrationEventHandler>(queueName: BillingConstant.QueueName);
    //    services.AddQueueHandler<TenantSubscribedIntegrationEventHandler>(queueName: BillingConstant.QueueName);
    //    return services;
    //}
}
