using Core.Context;

using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Sessions.RevokeAllSessions;

namespace Modules.Identity.Features.v1.Sessions.RevokeAllSessions;

public class RevokeAllSessionsCommandHandler(ISessionService sessionService, ICurrentUser currentUser)
    : ICommandHandler<RevokeAllSessionsCommand, int>
{
    public async ValueTask<int> Handle(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();
        return await sessionService.RevokeAllSessionsAsync(
            userId,
            userId,
            command.ExceptSessionId,
            "User requested logout from all devices",
            cancellationToken);
    }
}