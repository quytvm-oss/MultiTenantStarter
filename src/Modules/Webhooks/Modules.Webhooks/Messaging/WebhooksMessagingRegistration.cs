using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.Webhooks.Services;

using Web.MessageBus;

namespace Modules.Webhooks.Messaging;

public static class WebhooksMessagingRegistration
{
    public static IServiceCollection AddWebhooksMessaging(this IServiceCollection services, IConfiguration  configuration)
    {
        services.AddHeroMessaging(configuration, moduleKey: "webhooks");
        services.AddQueueHandler<WebhookFanoutHandler>(
            queueName: "webhooks",
            handlerKey: "webhooks.webhook-handler.v1");

        return services;
    }
}