using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Notifications.Contracts.v1.Queries;
using Modules.Notifications.Data;

namespace Modules.Notifications.Features.v1.GetUnreadCount;

public class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, int>
{
    private readonly NotificationsDbContext _notificationsDbContext;
    private readonly ICurrentUser _currentUser;
    public GetUnreadCountQueryHandler(NotificationsDbContext notificationsDbContext, ICurrentUser currentUser)
    {
        _notificationsDbContext = notificationsDbContext;
        _currentUser = currentUser;

    }

    public async ValueTask<int> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = _currentUser.GetUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var result = await _notificationsDbContext.Notifications
                        .AsNoTracking()
                        .CountAsync(x => x.UserId == currentUserId && x.ReadAtUtc == null, cancellationToken)
                        .ConfigureAwait(false);
        return result;
    }

}
