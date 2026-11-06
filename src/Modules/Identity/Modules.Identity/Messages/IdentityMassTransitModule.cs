using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Messaging.MassTransit.Abstractions;

using Modules.Identity.Events;

namespace Modules.Identity.Messages;

public class IdentityMassTransitModule : IMassTransitModule
{
    public void ConfigureEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.ReceiveEndpoint(
            IdentityModuleConstants.QueueName,
            e =>
            {
                e.PrefetchCount = 16;

                e.UseMessageRetry(r =>
                    r.Interval(3, TimeSpan.FromSeconds(2)));

                e.ConfigureConsumer<WebhookEventHandler>(context);
            });
    }

    public void RegisterConsumers(IBusRegistrationConfigurator registration)
    {
        registration.AddConsumer<WebhookEventHandler>();
    }
}
