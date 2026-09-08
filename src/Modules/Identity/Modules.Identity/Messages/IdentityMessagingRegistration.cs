using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.Identity.Events;

using Web.MessageBus;

namespace Modules.Identity.Messages;

public static class IdentityMessagingRegistration
{
    public static IServiceCollection AddIdentityMessaging(this IServiceCollection services, IConfiguration  configuration)
    {
        services.AddHeroMessaging(configuration, moduleKey: "identity");
        
        services.AddQueueHandler<WebhookEventHandler>(
            queueName: "identity",
            handlerKey: "identity.webhook-event.v1");

        return services;
    }
}