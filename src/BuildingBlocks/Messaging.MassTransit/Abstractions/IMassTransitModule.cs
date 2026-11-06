using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

namespace Messaging.MassTransit.Abstractions;

public interface IMassTransitModule
{
    void RegisterConsumers(IBusRegistrationConfigurator registration);

    void ConfigureEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context);
}
