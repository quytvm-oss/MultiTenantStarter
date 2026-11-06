using System;
using System.Collections.Generic;
using System.Text;

using Messaging.Rebus.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.Notifications.Events;

namespace Modules.Notifications.Messaging;

//public static class NotificationMessagingRegistration
//{
//    public static IServiceCollection AddNotificationMessaging(this IServiceCollection services, IConfiguration configuration)
//    {
//        services.AddHeroMessaging(configuration, moduleKey: NotificationModuleConstant.QueueName);
//        services.AddModuleBus<NotificationsModule>(NotificationModuleConstant.QueueName);

//        // register handlers
//        services.AddQueueHandler<InvoiceIssuedIntegrationEventHandler>(queueName: NotificationModuleConstant.QueueName);

//        return services;
//    }
//}