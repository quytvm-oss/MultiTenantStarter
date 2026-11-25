using Core.Context;

using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Tokens.Logout;

namespace Modules.Identity.Features.v1.Tokens.Logout;

public sealed class LogoutCommandHandler(ISessionService sessionService, ICurrentUser currentUser)
    : ICommandHandler<LogoutCommand, Unit>
{
    public async ValueTask<Unit> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();
        await sessionService.RevokeByRefreshTokenAsync(
            userId,command.RefreshToken,
            cancellationToken);
        return Unit.Value;
    }
}