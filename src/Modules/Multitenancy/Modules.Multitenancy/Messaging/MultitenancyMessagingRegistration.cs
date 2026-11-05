using System;
using System.Collections.Generic;
using System.Text;

using Messaging.Rebus.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Multitenancy.Messaging;

internal static class MultitenancyMessagingRegistration
{
    public static IServiceCollection AddMultitenancyMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHeroMessaging(configuration, moduleKey: MultitenancyModuleConstant.QueueName, isPrimary: false);
        services.AddModuleBus<MultitenancyModule>(MultitenancyModuleConstant.QueueName);

        return services;
    }
}
