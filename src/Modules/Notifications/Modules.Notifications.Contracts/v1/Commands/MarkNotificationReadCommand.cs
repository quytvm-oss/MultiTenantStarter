using Mediator;

namespace Modules.Notifications.Contracts.v1.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand<Unit>;
