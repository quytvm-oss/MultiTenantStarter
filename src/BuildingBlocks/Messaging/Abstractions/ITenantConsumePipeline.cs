using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

namespace Messaging.Abstractions;

public interface ITenantConsumePipeline
{
    void Configure(IReceiveEndpointConfigurator endpoint, IBusRegistrationContext context);
}
