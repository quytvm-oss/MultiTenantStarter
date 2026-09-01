using Core.Domain;

namespace Modules.Webhooks.Domain;

public class WebhookDelivery
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; private set; }

    public string EventType { get; private set; } = default!;

    public string PayloadJson { get; private set; } = default!;

    public int HttpStatusCode { get; private set; }

    public bool Success { get; private set; }

    public int AttemptCount { get; private set; } = 1;

    public DateTime AttemptedAtUtc { get; private set; }

    public string? ErrorMessage { get; private set; }

    private WebhookDelivery() { }

}
