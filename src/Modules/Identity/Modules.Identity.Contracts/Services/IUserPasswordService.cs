namespace Modules.Identity.Contracts.Services;

public interface IUserPasswordService
{
    Task ForgotPasswordAsync(string email, string origin, CancellationToken ct = default);
    
    Task ResetPasswordAsync(string email, string password, string token, CancellationToken ct = default);
    
    Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId, CancellationToken ct = default);
}