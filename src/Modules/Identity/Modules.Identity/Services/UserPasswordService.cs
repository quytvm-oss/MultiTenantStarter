using System.Collections.ObjectModel;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Jobs.Services;

using Mailling;
using Mailling.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

using Shared.Multitenancy;

namespace Modules.Identity.Services;

internal sealed class UserPasswordService(
    UserManager<User> userManager,
    IdentityDbContext db,
    IJobService jobService,
    IMailService mailService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IPasswordHistoryService passwordHistoryService,
    IPasswordExpiryService passwordExpiryService) : IUserPasswordService
{
    public async Task ForgotPasswordAsync(string email, string origin, CancellationToken ct = default)
    {
        EnsureValidTenant();
        
        var user =  await userManager.FindByEmailAsync(email);
        
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }
        
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        
        var resetPasswordUri = QueryHelpers.AddQueryString(
            $"{origin.TrimEnd('/')}/reset-password",
            new Dictionary<string, string?>
            {
                ["token"] = token,
                ["email"] = email,
                ["tenant"] = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id,
            });
        var mailRequest = new MailRequest(
            new Collection<string> { user.Email },
            "Reset Password",
            $"Please reset your password using the following link: {resetPasswordUri}");
        
        jobService.Enqueue(() => mailService.SendAsync(mailRequest, CancellationToken.None));
    }

    public async Task ResetPasswordAsync(string email, string password, string token, CancellationToken ct = default)
    {
        EnsureValidTenant();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new NotFoundException("user not found");
        }
        
        token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException("error resetting password", errors);
        }
        
        
    }

    public async Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("user not found");

        var result = await userManager.ChangePasswordAsync(user, password, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException("failed to change password", errors);
        }

        // Update password expiry date
        await passwordExpiryService.UpdateLastPasswordChangeDateAsync(userId, ct);

        // Save to history
        await passwordHistoryService.SavePasswordHistoryAsync(userId, ct);
    }

    #region internals

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
    }

    #endregion
}