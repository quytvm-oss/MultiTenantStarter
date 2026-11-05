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
        services.AddHeroMessaging(configuration, moduleKey: IdentityModuleConstants.QueueName);
        services.AddModuleBus<IdentityModule>(IdentityModuleConstants.QueueName);

        services.AddQueueHandler<WebhookEventHandler>(queueName: IdentityModuleConstants.QueueName);

        //services.AddMessageRoute<WebhookEvent>(IdentityModuleConstants.QueueName);

        return services;
    }
}