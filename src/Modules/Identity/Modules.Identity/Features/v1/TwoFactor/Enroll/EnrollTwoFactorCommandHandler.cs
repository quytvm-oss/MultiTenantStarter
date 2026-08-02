using System.Globalization;
using System.Text.Encodings.Web;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.AspNetCore.Identity;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.TwoFactor;
using Modules.Identity.Domain;

namespace Modules.Identity.Features.v1.TwoFactor.Enroll;

public class EnrollTwoFactorCommandHandler(UserManager<User> userManager, ICurrentUser currentUser)
    : ICommandHandler<EnrollTwoFactorCommand, TwoFactorEnrollmentResponse>
{
    private const string IssuerName = "Multitenant";

    public async ValueTask<TwoFactorEnrollmentResponse> Handle(EnrollTwoFactorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException();
        }
        
        var userId = currentUser.GetUserId().ToString();
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User with id {userId} not found");
        
        // Always reset so calling enroll twice rotates the secret — prevents stale codes
        // from a prior incomplete enrollment from silently succeeding.
        await userManager.ResetAuthenticatorKeyAsync(user);
        var sharedKey = await userManager.GetAuthenticatorKeyAsync(user)
            ?? throw new CustomException("Failed to generate authenticator key.");
        
        var email = user.Email ?? user.UserName ?? user.Id;
        var authenticatorUri = string.Format(
            CultureInfo.InvariantCulture, 
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            UrlEncoder.Default.Encode(IssuerName),
            UrlEncoder.Default.Encode(email),
            sharedKey);

        return new TwoFactorEnrollmentResponse(sharedKey, authenticatorUri);
    }
}