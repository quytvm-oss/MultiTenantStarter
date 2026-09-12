using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Notifications.Contracts.v1.Commands;
using Modules.Notifications.Data;

namespace Modules.Notifications.Features.v1.Notifications.MarkNotificationRead;

public class MarkNotificationReadCommandHandler : ICommandHandler<MarkNotificationReadCommand, Unit>
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _current;
    public MarkNotificationReadCommandHandler(NotificationsDbContext dbContext, ICurrentUser current)
    {
        _dbContext = dbContext;
        _current = current;

    }

    public async ValueTask<Unit> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var userId = _current.GetUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        // Caller-scoped: filter by (Id, UserId) so users can only mutate their own rows. Returns
        // 404 if the row exists but belongs to someone else — we don't leak existence.
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == command.NotificationId && x.UserId == currentUserId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Notification not found.");

        notification.MarkRead();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

}
