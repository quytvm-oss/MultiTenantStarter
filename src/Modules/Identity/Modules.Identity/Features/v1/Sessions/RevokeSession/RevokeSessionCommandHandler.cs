using Core.Context;

using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Sessions.RevokeSession;

namespace Modules.Identity.Features.v1.Sessions.RevokeSession;

public class RevokeSessionCommandHandler(ISessionService sessionService, ICurrentUser currentUser)
    : ICommandHandler<RevokeSessionCommand, bool>
{
    
    public async ValueTask<bool> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();
        return await sessionService.RevokeSessionAsync(command.SessionId,
            userId,
            "User requested",
            cancellationToken);
    }
}