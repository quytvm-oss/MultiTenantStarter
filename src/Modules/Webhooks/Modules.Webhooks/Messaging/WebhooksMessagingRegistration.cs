using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.Webhooks.Services;

using Messaging.Rebus.Configuration;

namespace Modules.Webhooks.Messaging;

public static class WebhooksMessagingRegistration
{
    public static IServiceCollection AddWebhooksMessaging(this IServiceCollection services, IConfiguration  configuration)
    {
        services.AddHeroMessaging(configuration, moduleKey: "webhooks");
        services.AddModuleBus<WebhooksModule>("webhooks");
        services.AddQueueHandler<WebhookFanoutHandler>(
            queueName: "webhooks");

        return services;
    }
}
