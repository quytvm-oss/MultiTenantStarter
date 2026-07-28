using Core.Context;

using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Sessions.GetMySessions;

namespace Modules.Identity.Features.v1.Sessions.GetMySessions;

public class GetMySessionsQueryHandler(ISessionService sessionService, ICurrentUser currentUser)
    : IQueryHandler<GetMySessionsQuery, List<UserSessionDto>>
{
    
    public async ValueTask<List<UserSessionDto>> Handle(GetMySessionsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();
        return await sessionService.GetUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}