using Hangfire;

namespace Modules.Webhooks.Services;

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public WebhookDispatcher(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }


    public Task EnqueueAsync(string tenantId, Guid subscriptionId, string eventType, string payloadJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        _backgroundJobClient.Enqueue<IWebhookDeliveryService>(x =>
        x.DeliverAsync(subscriptionId, tenantId, eventType, payloadJson, null!, CancellationToken.None));
        return Task.CompletedTask;
    }

}
