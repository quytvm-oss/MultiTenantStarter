using FluentValidation;

using Modules.Notifications.Contracts.v1.Queries;

namespace Modules.Notifications.Features.v1.Notifications.ListNotifications;

public class ListNotificationsQueryValidator : AbstractValidator<ListNotificationsQuery>
{
    public ListNotificationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
