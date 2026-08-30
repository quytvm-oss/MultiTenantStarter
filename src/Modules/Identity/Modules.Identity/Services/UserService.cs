using System.Security.Claims;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;

using Shared.Storage;

namespace Modules.Identity.Services;

internal sealed class UserService(
    IUserRegistrationService registrationService,
    IUserProfileService profileService,
    IUserStatusService statusService,
    IUserRoleService roleService,
    IUserPasswordService passwordService,
    IUserPermissionService permissionService) : IUserService
{
    public Task<bool> ExistsWithNameAsync(string name, CancellationToken ct = default)
        => profileService.ExistsWithNameAsync(name, ct);

    public Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null, CancellationToken ct = default)
        => profileService.ExistsWithEmailAsync(email, exceptId, ct);

    public Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null, CancellationToken ct = default)
        => profileService.ExistsWithPhoneNumberAsync(phoneNumber, exceptId, ct);

    public Task<List<UserDto>> GetListAsync(CancellationToken ct = default)
        => profileService.GetListAsync(ct);

    public Task<int> GetCountAsync(CancellationToken ct = default)
        => profileService.GetCountAsync(ct);

    public Task<UserDto> GetAsync(string userId, CancellationToken ct = default)
        => profileService.GetAsync(userId, ct);

    public Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken ct = default)
    => statusService.ToggleStatusAsync(activateUser, userId, ct);

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    => registrationService.GetOrCreateFromPrincipalAsync(principal, ct);

    public Task<string> RegisterAsync(string firstName, string lastName, string email, string userName, string password,
        string confirmPassword, string phoneNumber, string origin, CancellationToken ct = default)
    => registrationService.RegisterAsync(firstName, lastName, email, userName, password, confirmPassword, phoneNumber, origin, ct);

    public Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, FileUploadRequest image,
        bool deleteCurrentImage, CancellationToken ct = default)
    => profileService.UpdateAsync(userId, firstName, lastName, phoneNumber, image, deleteCurrentImage, ct);

    public Task DeleteAsync(string userId, CancellationToken ct = default)
    => statusService.DeleteAsync(userId, ct);

    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default)
        => registrationService.ConfirmEmailAsync(userId, code, tenant, ct);

    public Task AdminConfirmEmailAsync(string userId, CancellationToken ct = default)
    => registrationService.AdminConfirmEmailAsync(userId, ct);

    public Task ResendConfirmationEmailAsync(string userId, string origin, CancellationToken ct = default)
        => registrationService.ResendConfirmationEmailAsync(userId, origin, ct);

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default)
    => registrationService.ConfirmPhoneNumberAsync(userId, code, cancellationToken);

    public Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default)
    => permissionService.HasPermissionAsync(userId, permissionName, ct);

    public Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken)
    => passwordService.ForgotPasswordAsync(email, origin, cancellationToken);

    public Task ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken)
    => passwordService.ResetPasswordAsync(email, password, token, cancellationToken);

    public Task<List<string>> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    => permissionService.GetPermissionsAsync(userId, cancellationToken);

    public Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId,
        CancellationToken cancellationToken = default)
    => passwordService.ChangePasswordAsync(password, newPassword, confirmNewPassword, userId, cancellationToken);

    public Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken)
    => roleService.AssignRoleAsync(userId, userRoles, cancellationToken);

    public Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
   => roleService.GetUserRolesAsync(userId, cancellationToken);
}