using Core.Context;

using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Sessions.AdminRevokeAllSessions;

namespace Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;

public class AdminRevokeAllSessionsCommandHandler(ISessionService sessionService, ICurrentUser currentUser)
    : ICommandHandler<AdminRevokeAllSessionsCommand, int>
{

    public async ValueTask<int> Handle(AdminRevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var adminId = currentUser.GetUserId().ToString();
        return await sessionService.RevokeAllSessionsForAdminAsync(
            adminId,
            adminId,
            command.Reason ?? "Revoked by administrator", 
            cancellationToken);
    }
}