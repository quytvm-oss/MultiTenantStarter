namespace Modules.Identity.Contracts.Services;

public interface IPasswordHistoryService
{
    Task<bool> IsPasswordInHistoryAsync(string userId, string newPassword, CancellationToken ct = default);
    
    Task SavePasswordHistoryAsync(string userId, CancellationToken ct = default);
    
    Task CleanupOldPasswordHistoryAsync(string userId, CancellationToken ct = default);
}