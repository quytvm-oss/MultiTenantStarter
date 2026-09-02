using System.Net.Http.Headers;
using System.Text;

using Microsoft.Extensions.Logging;

using Modules.Webhooks.Data;
using Modules.Webhooks.Domain;

namespace Modules.Webhooks.Services;

public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebhookDbContext _dbContext;
    private readonly ILogger<WebhookDeliveryService> _logger;
    public WebhookDeliveryService(IHttpClientFactory httpClientFactory, WebhookDbContext dbContext, ILogger<WebhookDeliveryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _logger = logger;
    }
    public async Task DeliverAsync(Guid subscriptionId, string url, string? signingSecret, string eventType, string payloadJson, CancellationToken ct = default)
    {
        var delivery = WebhookDelivery.Create(subscriptionId, eventType, payloadJson);
        var client = _httpClientFactory.CreateClient("Webhook");

        try
        {
            using var content = new StringContent(payloadJson, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(signingSecret))
            {
                var signature = WebhookPayloadSigner.Sign(payloadJson, signingSecret);
                content.Headers.Add("X-Webhook-Signature", signature);
            }

            content.Headers.Add("X-Webhook-Event", eventType);
            content.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());

            var response = await client.PostAsync(url, content, ct).ConfigureAwait(false);
            delivery.RecordResult((int)response.StatusCode, response.IsSuccessStatusCode, null);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Webhook delivery {DeliveryId} to {Url} returned {StatusCode}",
                    delivery.Id, url, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            delivery.RecordResult(0, false, ex.Message);
            _logger.LogWarning(ex, "Webhook delivery {DeliveryId} to {Url} failed", delivery.Id, url);
        }

        _dbContext.WebhookDeliveries.Add(delivery);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

}
