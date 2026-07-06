using Mediator;

using MessageBus;

using Microsoft.Extensions.Logging;

using Modules.Identity.Contracts.Events;
using Modules.Identity.Domain.Events;

namespace Modules.Identity.Events;

public sealed class UserRegisteredHandler(
    IBusPublisher eventBus,
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
            TenantId: notification.TenantId,
            UserId: notification.UserId,
            Email: notification.Email,
            Source: notification.Source ?? string.Empty,
            FirstName: notification.FirstName ?? string.Empty,
            LastName: notification.LastName ?? string.Empty);

        await eventBus.PublishAsync(integrationEvent,x =>
        {
            x.Name = "user.register";
            x.Source = "Identity";
            x.TenantId = notification.TenantId;
        }, cancellationToken);
    }
}
