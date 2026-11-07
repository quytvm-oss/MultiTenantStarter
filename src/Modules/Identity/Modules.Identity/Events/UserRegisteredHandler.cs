using MassTransit;

using Mediator;

using Messaging.Persistence;

using Microsoft.Extensions.Logging;

using Modules.Identity.Contracts.Events;
using Modules.Identity.Domain.Events;

namespace Modules.Identity.Events;

public sealed class UserRegisteredHandler(
    ISendEndpointProvider sendEndpointProvider,
    MessagingDbContext messagingDbContext,
    ILogger<UserRegisteredHandler> logger)
    : INotificationHandler<UserRegisteredEvent>
{
    public async ValueTask Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            // PII minimization: log the pseudonymous UserId only, not the email address.
            logger.LogInformation("User registered: {UserId}", notification.UserId);
        }

        var integrationEvent = new UserRegisteredIntegrationEvent(
            Id: notification.EventId,
            OccurredOnUtc: notification.OccurredOnUtc.UtcDateTime,
            Source: notification.Source ?? string.Empty,
            CorrelationId: notification.CorrelationId ?? string.Empty,
            TenantId: notification.TenantId,
            UserId: notification.UserId,
            Email: notification.Email,
            FirstName: notification.FirstName ?? string.Empty,
            LastName: notification.LastName ?? string.Empty);
        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:notification"));
        await sendEndpointProvider.Send(integrationEvent);
        await messagingDbContext.SaveChangesAsync(cancellationToken);
    }
}
