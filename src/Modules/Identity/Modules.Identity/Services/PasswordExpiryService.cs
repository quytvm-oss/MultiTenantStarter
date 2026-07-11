using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

namespace Modules.Identity.Services;

public class PasswordExpiryService(
    UserManager<User> userManager,
    IOptions<PasswordPolicyOptions> passwordPolicyOptions,
    TimeProvider timeProvider)
    : IPasswordExpiryService
{
    private readonly PasswordPolicyOptions _passwordPolicyOptions = passwordPolicyOptions.Value;

    public async Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await  userManager.FindByIdAsync(userId);
        
        if (user is null)
            return false;

        return IsPasswordExpired(user);
    }

    public async Task<int> GetDaysUntilExpiryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await  userManager.FindByIdAsync(userId);
        
        if (user is null)
            return int.MaxValue;
        
        return GetDaysUntilExpiry(user);
    }

    public async Task<bool> IsPasswordExpiringWithinWarningPeriodAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        return IsPasswordExpiringWithinWarningPeriod(user);
    }

    public async Task<PasswordExpiryStatusDto> GetPasswordExpiryStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new PasswordExpiryStatusDto
            {
                IsExpired = false,
                IsExpiringWithinWarningPeriod = false,
                DaysUntilExpiry = int.MaxValue,
                ExpiryDate = null
            };
        }

        return GetPasswordExpiryStatus(user);
    }

    public async Task UpdateLastPasswordChangeDateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            user.LastPasswordChangeDateTime = timeProvider.GetUtcNow().UtcDateTime;
            await userManager.UpdateAsync(user);
        }
    }

    #region internals

    private int GetDaysUntilExpiry(User user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return int.MaxValue;
        }  
        
        var expiryDate = user.LastPasswordChangeDateTime.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        return (int)Math.Ceiling((expiryDate - timeProvider.GetUtcNow().UtcDateTime).TotalDays);
    }

    private bool IsPasswordExpired(User user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return false;
        }

        var expiryDate = user.LastPasswordChangeDateTime.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        return timeProvider.GetUtcNow().UtcDateTime > expiryDate;
    }

    private bool IsPasswordExpiringWithinWarningPeriod(User user)
    {
        if (!_passwordPolicyOptions.EnforcePasswordExpiry)
        {
            return false;
        }
        
        var daysUntilExpiry = GetDaysUntilExpiry(user);
        return daysUntilExpiry >= 0 && daysUntilExpiry <= _passwordPolicyOptions.PasswordExpiryWarningDays;
    }

    private PasswordExpiryStatusDto GetPasswordExpiryStatus(User user)
    {
        var expiryDate = user.LastPasswordChangeDateTime.AddDays(_passwordPolicyOptions.PasswordExpiryDays);
        var daysUntilExpiry = GetDaysUntilExpiry(user);
        var isExpired = IsPasswordExpired(user);
        var isExpiringWithinWarningPeriod = IsPasswordExpiringWithinWarningPeriod(user);

        return new PasswordExpiryStatusDto
        {
            IsExpired = isExpired,
            IsExpiringWithinWarningPeriod = isExpiringWithinWarningPeriod,
            DaysUntilExpiry = daysUntilExpiry,
            ExpiryDate = _passwordPolicyOptions.EnforcePasswordExpiry ? expiryDate : null
        };
    }

    #endregion
}