using Core.Context;

using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Sessions.AdminRevokeSession;

namespace Modules.Identity.Features.v1.Sessions.AdminRevokeSession;

public class AdminRevokeSessionCommandHandler(ISessionService sessionService, ICurrentUser currentUser)
    : ICommandHandler<AdminRevokeSessionCommand, bool>
{

    public async ValueTask<bool> Handle(AdminRevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var adminId = currentUser.GetUserId().ToString();

        var session = await sessionService.GetSessionAsync(command.SessionId, cancellationToken);
        if (session == null || session.UserId != command.UserId.ToString())
            return false;

        return await sessionService.RevokeSessionForAdminAsync(
            command.SessionId,
            adminId,
            command.Reason ?? "Revoked by administrator",
            cancellationToken);
    }
}