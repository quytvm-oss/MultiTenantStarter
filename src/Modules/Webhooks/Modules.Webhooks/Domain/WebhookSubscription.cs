using Core.Domain;

namespace Modules.Webhooks.Domain;

public class WebhookSubscription : AggregateRoot<Guid>
{
    public string Url { get; private set; } = default!;
    public string EventsCsv { get; private set; } = default!;
    public string? ProtectedSecret { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    private WebhookSubscription() { }

    public string[] GetEvents() =>
        EventsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public bool MatchesEvent(string eventType) =>
        GetEvents().Contains(eventType, StringComparer.OrdinalIgnoreCase) ||
        GetEvents().Contains("*");

    public void Deactivate() => IsActive = false;
}
