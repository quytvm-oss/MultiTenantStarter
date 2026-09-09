using System.Text.Json;

using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Webhooks.Contracts.v1.TestWebhookSubscription;
using Modules.Webhooks.Data;
using Modules.Webhooks.Services;

namespace Modules.Webhooks.Features.v1.TestWebhookSubscription;

public class TestWebhookSubscriptionCommandHandler : ICommandHandler<TestWebhookSubscriptionCommand, bool>
{
    private readonly WebhookDbContext _dbContext;
    private readonly IWebhookDeliveryService _webhookDeliveryService;
    private readonly IWebhookSecretProtector _webhookSecretProtector;
    public TestWebhookSubscriptionCommandHandler(WebhookDbContext dbContext, IWebhookDeliveryService webhookDeliveryService, IWebhookSecretProtector webhookSecretProtector)
    {
        _dbContext = dbContext;
        _webhookDeliveryService = webhookDeliveryService;
        _webhookSecretProtector = webhookSecretProtector;
    }

    public async ValueTask<bool> Handle(TestWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await _dbContext.WebhookSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new NotFoundException($"Webhook subscription {command.Id} not found.");

        var testPayload = JsonSerializer.Serialize(new
        {
            eventType = "webhook.test",
            timestamp = TimeProvider.System.GetUtcNow().UtcDateTime,
            message = "This is a test webhook delivery."
        });

        await _webhookDeliveryService.DeliverAsync(
            subscription.Id,
            subscription.Url,
            _webhookSecretProtector.Unprotect(subscription.ProtectedSecret),
            "webhook.test",
            testPayload,
            cancellationToken
        ).ConfigureAwait(false);

        return true;
    }

}
