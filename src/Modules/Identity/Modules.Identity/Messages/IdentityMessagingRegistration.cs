using Messaging.Rebus.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.Identity.Events;

using Shared.Webhooks;

namespace Modules.Identity.Messages;

public static class IdentityMessagingRegistration
{
    public static IServiceCollection AddIdentityMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHeroMessaging(configuration, moduleKey: "identity");
        services.AddModuleBus<IdentityModule>("identity");

        services.AddQueueHandler<WebhookEventHandler>(queueName: "identity");

        //services.AddMessageRoute<WebhookEvent>("identity");

        return services;
    }
}