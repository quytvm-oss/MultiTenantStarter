using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

namespace Modules.Identity.Services;

public class PasswordHistoryService(
    IdentityDbContext db,
    UserManager<User> userManager,
    IOptions<PasswordPolicyOptions> passwordPolicyOptions)
    : IPasswordHistoryService
{
    private readonly PasswordPolicyOptions _passwordPolicyOptions = passwordPolicyOptions.Value;

    public async Task<bool> IsPasswordInHistoryAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(newPassword);
        
        var user = await userManager.FindByIdAsync(userId);
        
        if (user is null)
            return false;
        
        var passwordHistoryCount = _passwordPolicyOptions.PasswordHistoryCount;
        
        if (passwordHistoryCount <= 0)
            return false;

        var recentPasswordHashes = await db.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(passwordHistoryCount)
            .Select(ph => ph.PasswordHash)
            .ToListAsync(ct);

        foreach (var passwordHash in recentPasswordHashes)
        {
            var passwordHasher = userManager.PasswordHasher;
            var result = passwordHasher.VerifyHashedPassword(user, passwordHash, newPassword);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                return true;
        }
        
        return false;
    }

    public async Task SavePasswordHistoryAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var user = await userManager.FindByIdAsync(userId);
        
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return;
        }
        
        var passwordHistoryEntry =  PasswordHistory.Create(user.Id, user.PasswordHash); 
        
        await db.Set<PasswordHistory>().AddAsync(passwordHistoryEntry, ct);
        await db.SaveChangesAsync(ct);
        
        await CleanupOldPasswordHistoryAsync(userId, ct);
    }

    public async Task CleanupOldPasswordHistoryAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var passwordHistoryCount = _passwordPolicyOptions.PasswordHistoryCount;
        
        if (passwordHistoryCount <= 0)
            return;

        var allPasswordHistories = await db.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        if (allPasswordHistories.Count > passwordHistoryCount)
        {
            var oldPasswordHistories = allPasswordHistories.Skip(passwordHistoryCount).ToList();
            db.Set<PasswordHistory>().RemoveRange(oldPasswordHistories);
            await db.SaveChangesAsync(ct);
        }
    }
}