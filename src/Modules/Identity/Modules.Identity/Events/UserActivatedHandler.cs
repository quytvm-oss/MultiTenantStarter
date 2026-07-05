using Mediator;

using Microsoft.Extensions.Logging;

using Modules.Identity.Domain.Events;

namespace Modules.Identity.Events;

/// <summary>
/// Handles the UserActivatedEvent domain event.
/// </summary>
public sealed class UserActivatedHandler(
    ILogger<UserActivatedHandler> logger)
    : INotificationHandler<UserActivatedEvent>
{
    public ValueTask Handle(UserActivatedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "User {UserId} activated by {ActivatedBy}",
                notification.UserId,
                notification.ActivatedBy);
        }

        return ValueTask.CompletedTask;
    }
}
