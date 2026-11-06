using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Messaging.Abstractions;

using Modules.Identity.Data;
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

                e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
                e.UseEntityFrameworkOutbox<IdentityDbContext>(context);
                e.ConfigureConsumer<WebhookEventHandler>(context);
            });
    }

    public void RegisterOutbox(IBusRegistrationConfigurator cfg) 
    {
        cfg.AddEntityFrameworkOutbox<IdentityDbContext>(o =>
        {
            o.UsePostgres();
            o.UseBusOutbox();
            o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
        });
    }

    public void RegisterConsumers(IBusRegistrationConfigurator registration)
    {
        registration.AddConsumer<WebhookEventHandler>();
    }
}
