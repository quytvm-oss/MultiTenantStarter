using Messaging.Rebus.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Webhooks.Messaging;

public static class WebhooksMessagingRegistration
{
    public static IServiceCollection AddWebhooksMessaging(this IServiceCollection services, IConfiguration  configuration)
    {
        services.AddHeroMessaging(configuration, moduleKey: "webhooks");
        //services.AddQueueHandler<WebhookFanoutHandler>(queueName: "webhooks");

        return services;
    }
}