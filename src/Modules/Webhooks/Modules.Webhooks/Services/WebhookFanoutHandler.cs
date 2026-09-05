using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

using Modules.Webhooks.Data;

using Rebus.Handlers;

using Shared.Multitenancy;
using Shared.Webhooks;

namespace Modules.Webhooks.Services;

/// <summary>
/// Open-generic bridge that fans every published integration event out to the
/// tenant's active webhook subscriptions, then enqueues a delivery job per
/// subscription via <see cref="IWebhookDispatcher"/>.
///
/// Registered as an open generic in <c>WebhooksModule</c> so DI materializes
/// a closed handler for any <typeparamref name="TEvent"/> the event bus
/// publishes — no per-event wiring required.
///
/// Tenant scoping is two-layer: the event's own <c>TenantId</c> selects the
/// row scope, and the DbContext's Finbuckle query filter then constrains the
/// subscription read to that tenant. Events with a null <c>TenantId</c> are
/// skipped — webhook subscriptions are tenant-only by design.
/// </summary>
public class WebhookFanoutHandler : IHandleMessages<WebhookEvent>

{
    private readonly WebhookDbContext _db;
    private readonly IWebhookDispatcher _dispatcher;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantContextAccessor;
    private readonly ILogger<WebhookFanoutHandler> _logger;
    public WebhookFanoutHandler(WebhookDbContext db, IWebhookDispatcher dispatcher,
        IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor,
        ILogger<WebhookFanoutHandler> logger)
    {
        _db = db;
        _dispatcher = dispatcher;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;

    }

    public async Task Handle(WebhookEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation(
            "Webhook fan-out for event {EventType} (tenant {TenantId}, correlation {CorrelationId})",
            typeof(WebhookEvent).Name,
            message.TenantId,
            message.CorrelationId);

        if (string.IsNullOrWhiteSpace(message.TenantId))
        {
            // Global events are not deliverable via tenant-scoped subscriptions.
            return;
        }

        // Install the tenant context for the subscription read — the WebhookDbContext Finbuckle filter needs
        // it, and the background event pumps (OutboxDispatcher / event bus) carry no HTTP context.
        var prev = _tenantContextAccessor.MultiTenantContext;
        try
        {
            var info = new AppTenantInfo(message.TenantId, message.TenantId);
            ((IMultiTenantContextSetter)_tenantContextAccessor).MultiTenantContext =
                new MultiTenantContext<AppTenantInfo>(info);

            var eventType = message.EventType;

            // Pull active subscriptions, then match event type in memory: EventsCsv is a CSV blob (no join
            // table), and there are typically 0–20 subscriptions per tenant so in-memory matching is fine.
            var subscriptions = await _db.WebhookSubscriptions
                                .IgnoreQueryFilters()
                                .AsNoTracking()
                                .Where(s => s.IsActive)
                                .ToListAsync();

            var matching = subscriptions.Where(s => s.MatchesEvent(eventType)).ToList();
            if (matching.Count == 0)
            {
                return;
            }

            var payload = message.Payload;
            foreach (var subscription in matching)
            {
                try
                {
                    await _dispatcher
                        .EnqueueAsync(message.TenantId, subscription.Id, eventType, payload, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One bad subscription must not abort fan-out to others; this catches synchronous
                    // enqueue-side failures (Hangfire transient errors etc), not delivery (the job retries).
                    _logger.LogWarning(
                        ex,
                        "Failed to enqueue webhook delivery for subscription {SubscriptionId} (tenant {TenantId}, event {EventType})",
                        subscription.Id,
                        message.TenantId,
                        eventType);
                }
            }
        }
        finally
        {
            ((IMultiTenantContextSetter)_tenantContextAccessor).MultiTenantContext = prev;
        }
    }
}
