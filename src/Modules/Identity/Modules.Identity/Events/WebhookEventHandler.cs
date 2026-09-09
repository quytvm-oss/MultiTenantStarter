using Microsoft.Extensions.Logging;

using Rebus.Handlers;

using Shared.Webhooks;

namespace Modules.Identity.Events;

public class WebhookEventHandler(ILogger<WebhookEventHandler> logger) : IHandleMessages<WebhookEvent>
{
    public Task Handle(WebhookEvent message)
    {
        logger.LogInformation("Received WebhookEvent: {EventType} with Payload: {Payload}", message.EventType, message.Payload);
        return Task.CompletedTask;
    }
}
