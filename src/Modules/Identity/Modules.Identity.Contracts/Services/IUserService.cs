using System.Security.Claims;

using Modules.Identity.Contracts.DTOs;

using Shared.Storage;

namespace Modules.Identity.Contracts.Services;

public interface IUserService
{
    Task<bool> ExistsWithNameAsync(string name, CancellationToken ct = default);
    
    Task<bool> ExistsWithEmailAsync(string email,string? exceptId = null, CancellationToken ct = default);
    
    Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null, CancellationToken ct = default);
    
    Task<List<UserDto>> GetListAsync(CancellationToken ct = default);
    
    Task<int> GetCountAsync(CancellationToken ct = default);
    
    Task<UserDto> GetAsync(string userId, CancellationToken ct = default);
    
    Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken ct = default);

    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default);

    Task<string> RegisterAsync(string firstName, string lastName, string email,string userName, string password,string confirmPassword, string phoneNumber, string origin,
        CancellationToken ct = default);

    Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, StreamUploadRequest image,
        bool deleteCurrentImage, CancellationToken ct = default);
    
    Task DeleteAsync(string userId, CancellationToken ct = default);
    
    Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default);
    
    Task AdminConfirmEmailAsync(string userId, CancellationToken ct = default);
    
    Task ResendConfirmationEmailAsync(string userId, string origin, CancellationToken ct = default);
    
    Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default);
    
    // permisions
    Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default);
    
    //password
    Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken);
    
    Task ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken);
    
    Task<List<string>> GetPermissionsAsync(string userId, CancellationToken cancellationToken);

    Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId, CancellationToken cancellationToken = default);
    
    Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken);
    
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken);
}