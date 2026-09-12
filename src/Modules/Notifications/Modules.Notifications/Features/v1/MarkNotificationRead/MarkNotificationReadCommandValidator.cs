using FluentValidation;

using Modules.Notifications.Contracts.v1.Commands;

namespace Modules.Notifications.Features.v1.MarkNotificationRead;

public class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
