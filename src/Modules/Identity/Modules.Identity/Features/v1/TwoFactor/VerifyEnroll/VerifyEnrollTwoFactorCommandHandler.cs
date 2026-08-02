using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.AspNetCore.Identity;

using Modules.Identity.Contracts.v1.TwoFactor;
using Modules.Identity.Domain;

namespace Modules.Identity.Features.v1.TwoFactor.VerifyEnroll;

public class VerifyEnrollTwoFactorCommandHandler(UserManager<User> userManager, ICurrentUser currentUser)
    : ICommandHandler<VerifyEnrollTwoFactorCommand, bool>
{
    

    public async ValueTask<bool> Handle(VerifyEnrollTwoFactorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }
        
        var userId = currentUser.GetUserId().ToString();
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException($"User with id {userId} not found");
        
        var sanitized = command.Code.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            sanitized);
        
        if (!valid)
        {
            throw new CustomException(
                "The authenticator code is invalid.",
                errors: null,
                System.Net.HttpStatusCode.BadRequest);
        }
        
        await userManager.SetTwoFactorEnabledAsync(user, true);

        return true;
    }
}