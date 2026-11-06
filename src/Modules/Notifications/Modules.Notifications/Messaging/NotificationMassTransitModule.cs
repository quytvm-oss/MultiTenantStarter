using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Messaging.MassTransit.Abstractions;

using Modules.Notifications.Events;

namespace Modules.Notifications.Messaging;

public class NotificationMassTransitModule : IMassTransitModule
{
    public void ConfigureEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.ReceiveEndpoint(
            NotificationModuleConstant.QueueName,
            e =>
            {
                e.PrefetchCount = 16;

                e.UseMessageRetry(r =>
                    r.Interval(3, TimeSpan.FromSeconds(2)));

                e.ConfigureConsumer<InvoiceIssuedIntegrationEventHandler>(context);
            });
    }

    public void RegisterConsumers(IBusRegistrationConfigurator registration)
    {
        registration.AddConsumer<InvoiceIssuedIntegrationEventHandler>();
    }
}
