using Mediator;

using Microsoft.Extensions.Logging;

using Modules.Identity.Contracts.Events;
using Modules.Identity.Domain.Events;

using Rebus.Bus;

namespace Modules.Identity.Events;

public sealed class UserRegisteredHandler(
    IBus bus,
    ILogger<UserRegisteredHandler> logger)
    : INotificationHandler<UserRegisteredEvent>
{
    public async ValueTask Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            // PII minimization: log the pseudonymous UserId only, not the email address.
            logger.LogInformation(
                "User registered: {UserId}",
                notification.UserId);
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

        await bus.Send(integrationEvent);
    }
}
