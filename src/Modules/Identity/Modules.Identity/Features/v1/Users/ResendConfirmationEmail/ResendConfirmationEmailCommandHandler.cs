using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.ResendConfirmationEmail;

namespace Modules.Identity.Features.v1.Users.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailCommandHandler(IUserService userService)
    : ICommandHandler<ResendConfirmationEmailCommand, Unit>
{
    public async ValueTask<Unit> Handle(ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await userService.ResendConfirmationEmailAsync(command.UserId, command.Origin, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
