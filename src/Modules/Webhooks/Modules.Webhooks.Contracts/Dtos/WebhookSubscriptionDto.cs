namespace Modules.Webhooks.Contracts.Dtos;

public class WebhookSubscriptionDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = default!;
    public string[] Events { get; init; } = [];
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
