using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.AspNetCore.Identity;

using Modules.Identity.Contracts.v1.TwoFactor;
using Modules.Identity.Domain;

namespace Modules.Identity.Features.v1.TwoFactor.Disable;

public class DisableTwoFactorCommandHandler(UserManager<User> userManager, ICurrentUser currentUser)
    : ICommandHandler<DisableTwoFactorCommand, bool>
{

    public async ValueTask<bool> Handle(DisableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }
        
        var userId = currentUser.GetUserId().ToString();
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException($"User {userId} not found.");
        
        // Require current password so a stolen access token alone can't downgrade
        // account security.
        if (!(await userManager.CheckPasswordAsync(user, command.CurrentPassword)))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }
        
        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        
        return true;
    }
}