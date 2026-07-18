using Mediator;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Users.ResetPassword;

namespace Modules.Identity.Features.v1.Users.ResetPassword;

public sealed class ResetPasswordCommandHandler(IUserService userService)
    : ICommandHandler<ResetPasswordCommand, string>
{
    public async ValueTask<string> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await userService.ResetPasswordAsync(command.Email, command.Password, command.Token, cancellationToken).ConfigureAwait(false);

        return "Password has been reset.";
    }
}