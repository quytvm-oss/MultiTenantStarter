using System.Collections.ObjectModel;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Notifications.Contracts.Dtos;

using Modules.Notifications.Contracts.v1.Queries;
using Modules.Notifications.Data;

namespace Modules.Notifications.Features.v1.ListNotifications;

public class ListNotificationsQueryHandler : IQueryHandler<ListNotificationsQuery, ReadOnlyCollection<NotificationDto>>
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    public ListNotificationsQueryHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;

    }
    public async ValueTask<ReadOnlyCollection<NotificationDto>> Handle(ListNotificationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = _currentUser.GetUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryFilter = _dbContext.Notifications.AsNoTracking()
            .Where(n => n.UserId == currentUserId);

        if (query.UnreadOnly)
        {
            queryFilter = queryFilter.Where(n => n.ReadAtUtc == null);
        }

        var rows = await queryFilter
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(n => n.ToDto()).ToList().AsReadOnly();
    }

}
