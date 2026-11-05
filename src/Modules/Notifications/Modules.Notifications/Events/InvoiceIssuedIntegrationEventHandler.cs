using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Logging;

using Modules.Billing.Contracts.Events;

using Rebus.Handlers;

namespace Modules.Notifications.Events;

public sealed class InvoiceIssuedIntegrationEventHandler : IHandleMessages<InvoiceIssuedIntegrationEvent>
{
    private readonly ILogger<InvoiceIssuedIntegrationEventHandler> _logger;

    public InvoiceIssuedIntegrationEventHandler(ILogger<InvoiceIssuedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(InvoiceIssuedIntegrationEvent message)
    {
        _logger.LogInformation("Received InvoiceIssuedIntegrationEvent: {InvoiceId}, {TenantId}, {Amount}, {IssuedAt}",
            message.InvoiceId, message.TenantId, message.Amount, message.OccurredOnUtc);
    }
}
