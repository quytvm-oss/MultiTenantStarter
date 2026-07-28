using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Sessions.GetUserSessions;

namespace Modules.Identity.Features.v1.Sessions.GetUserSessions;

public class GetUserSessionsQueryHandler(ISessionService sessionService)
    : IQueryHandler<GetUserSessionsQuery, List<UserSessionDto>>
{
    public async ValueTask<List<UserSessionDto>> Handle(GetUserSessionsQuery query, CancellationToken cancellationToken)
    {
        return await sessionService.GetUserSessionsForAdminAsync(query.UserId.ToString(), cancellationToken)
            .ConfigureAwait(false);
    }
}