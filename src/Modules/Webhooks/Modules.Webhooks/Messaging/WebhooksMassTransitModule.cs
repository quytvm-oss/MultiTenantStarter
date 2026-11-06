using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Messaging.MassTransit.Abstractions;

using Modules.Webhooks.Services;

namespace Modules.Webhooks.Messaging;

public class WebhooksMassTransitModule : IMassTransitModule
{
    public void ConfigureEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.ReceiveEndpoint(
            WebhooksModuleConstant.QueueName,
            e =>
            {
                e.PrefetchCount = 16;

                e.UseMessageRetry(r =>
                    r.Interval(3, TimeSpan.FromSeconds(2)));

                e.ConfigureConsumer<WebhookFanoutHandler>(context);
            });
    }

    public void RegisterConsumers(IBusRegistrationConfigurator registration)
    {
        registration.AddConsumer<WebhookFanoutHandler>();
    }
}
