using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Messaging.Abstractions;

using Modules.Multitenancy.Filters;

namespace Modules.Multitenancy.Services;

public sealed class TenantConsumePipeline : ITenantConsumePipeline
{
    public void Configure(IReceiveEndpointConfigurator endpoint, IBusRegistrationContext context)
    {
        endpoint.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
    }
}
