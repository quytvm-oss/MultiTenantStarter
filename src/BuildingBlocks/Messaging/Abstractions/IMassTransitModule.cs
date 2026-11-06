using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

namespace Messaging.Abstractions;

public interface IMassTransitModule
{
    void RegisterConsumers(IBusRegistrationConfigurator registration);

    void RegisterOutbox(IBusRegistrationConfigurator cfg) { }

    void ConfigureEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context);
}
