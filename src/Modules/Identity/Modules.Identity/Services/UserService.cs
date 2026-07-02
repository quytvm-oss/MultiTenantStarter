using System.Security.Claims;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;

using Shared.Storage;

namespace Modules.Identity.Services;

public class UserService : IUserService
{
    public Task<bool> ExistsWithNameAsync(string name, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserDto>> GetListAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetCountAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserDto> GetAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> RegisterAsync(string firstName, string lastName, string email, string userName, string password,
        string confirmPassword, string phoneNumber, string origin, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, BufferedUploadRequest image,
        bool deleteCurrentImage, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AdminConfirmEmailAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> ResendConfirmationEmailAsync(string userId, string origin, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}