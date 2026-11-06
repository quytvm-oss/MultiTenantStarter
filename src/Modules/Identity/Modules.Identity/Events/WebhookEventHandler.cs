using MassTransit;

using Microsoft.Extensions.Logging;

using Rebus.Handlers;
using Rebus.Messages;

using Shared.Webhooks;

namespace Modules.Identity.Events;

public class WebhookEventHandler(ILogger<WebhookEventHandler> logger) : IConsumer<WebhookEvent>
{
    public Task Consume(ConsumeContext<WebhookEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Received WebhookEvent: {EventType} with Payload: {Payload}", message.EventType, message.Payload);
        return Task.CompletedTask;
    }

}
