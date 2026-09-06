using Microsoft.Extensions.Logging;

using Rebus.Handlers;

using Shared.Webhooks;

namespace Modules.Identity.Events;

public class WebhookEventHandler : IHandleMessages<WebhookEvent>
{
    private readonly ILogger<WebhookEventHandler> _logger;

    public WebhookEventHandler(ILogger<WebhookEventHandler> logger)
    {
        _logger = logger;
    }


    public Task Handle(WebhookEvent message)
    {
        _logger.LogInformation("Received WebhookEvent: {EventType} with Payload: {Payload}", message.EventType, message.Payload);
        return Task.CompletedTask;
    }
}
