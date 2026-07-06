using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

namespace Modules.Identity.Services;

public class PasswordHistoryService : IPasswordHistoryService
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly PasswordPolicyOptions _passwordPolicyOptions;

    public PasswordHistoryService(
        IdentityDbContext db,
        UserManager<User> userManager,
        IOptions<PasswordPolicyOptions> passwordPolicyOptions)
    {
        _db = db;
        _userManager = userManager;
        _passwordPolicyOptions = passwordPolicyOptions.Value;
    }
    
    public async Task<bool> IsPasswordInHistoryAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(newPassword);
        
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user is null)
            return false;
        
        var passwordHistoryCount = _passwordPolicyOptions.PasswordHistoryCount;
        
        if (passwordHistoryCount <= 0)
            return false;

        var recentPasswordHashes = await _db.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(passwordHistoryCount)
            .Select(ph => ph.PasswordHash)
            .ToListAsync(ct);

        foreach (var passwordHash in recentPasswordHashes)
        {
            var passwordHasher = _userManager.PasswordHasher;
            var result = passwordHasher.VerifyHashedPassword(user, passwordHash, newPassword);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                return true;
        }
        
        return false;
    }

    public async Task SavePasswordHistoryAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return;
        }
        
        var passwordHistoryEntry =  PasswordHistory.Create(user.Id, user.PasswordHash); 
        
        await _db.Set<PasswordHistory>().AddAsync(passwordHistoryEntry, ct);
        await _db.SaveChangesAsync(ct);
        
        await CleanupOldPasswordHistoryAsync(userId, ct);
    }

    public async Task CleanupOldPasswordHistoryAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var passwordHistoryCount = _passwordPolicyOptions.PasswordHistoryCount;
        
        if (passwordHistoryCount <= 0)
            return;

        var allPasswordHistories = await _db.Set<PasswordHistory>()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        if (allPasswordHistories.Count > passwordHistoryCount)
        {
            var oldPasswordHistories = allPasswordHistories.Skip(passwordHistoryCount).ToList();
            _db.Set<PasswordHistory>().RemoveRange(oldPasswordHistories);
            await _db.SaveChangesAsync(ct);
        }
    }
}