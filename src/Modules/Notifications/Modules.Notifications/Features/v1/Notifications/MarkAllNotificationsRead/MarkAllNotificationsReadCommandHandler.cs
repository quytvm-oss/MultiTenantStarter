using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Notifications.Contracts.v1.Commands;
using Modules.Notifications.Data;

namespace Modules.Notifications.Features.v1.Notifications.MarkAllNotificationsRead;

public class MarkAllNotificationsReadCommandHandler : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    public MarkAllNotificationsReadCommandHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;

    }
    public async ValueTask<int> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var userId = _currentUser.GetUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var now = DateTime.UtcNow;
        var countUpdated = await _dbContext.Notifications
                .Where(x => x.UserId == currentUserId && x.ReadAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, now), cancellationToken)
                .ConfigureAwait(false);
        return countUpdated;
    }

}
