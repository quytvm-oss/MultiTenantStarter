using System;
using System.Collections.Generic;
using System.Text;

using Core.Messaging;

namespace Modules.Webhooks.Contracts.Events;

public sealed record WebhookEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    string EventType,
    string FullName,
    string Payload,
    string UserId) : IIntegrationEvent;