using System;
using System.Collections.Generic;
using System.Text;

using MassTransit;

using Microsoft.Extensions.Logging;

using Modules.Billing.Contracts.Events;

namespace Modules.Notifications.Events;

public sealed class InvoiceIssuedIntegrationEventHandler : IConsumer<InvoiceIssuedIntegrationEvent>
{
    private readonly ILogger<InvoiceIssuedIntegrationEventHandler> _logger;

    public InvoiceIssuedIntegrationEventHandler(ILogger<InvoiceIssuedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<InvoiceIssuedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received InvoiceIssuedIntegrationEvent: {InvoiceId}, {TenantId}, {Amount}, {IssuedAt}",
            message.InvoiceId, message.TenantId, message.Amount, message.OccurredOnUtc);
        return Task.CompletedTask;
    }
}
