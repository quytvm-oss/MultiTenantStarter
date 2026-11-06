using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Messaging.MassTransit.Abstractions;

namespace Modules.Multitenancy.Messaging;

public sealed class MutitenancyMassTransitModule : IMassTransitModule
{
    public void ConfigureEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.ReceiveEndpoint(
            MultitenancyModuleConstant.QueueName,
            e =>
            {
                e.PrefetchCount = 16;

                e.UseMessageRetry(r =>
                    r.Interval(3, TimeSpan.FromSeconds(2)));
            });
    }

    public void RegisterConsumers(IBusRegistrationConfigurator registration)
    {
        
    }
}
