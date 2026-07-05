namespace Modules.Identity.Contracts.Services;

public interface IUserStatusService
{
    Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken ct = default);
    
    Task DeleteAsync(string userId, CancellationToken ct = default);
}