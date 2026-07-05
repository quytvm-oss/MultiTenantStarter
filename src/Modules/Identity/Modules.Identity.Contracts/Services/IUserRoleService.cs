using Modules.Identity.Contracts.DTOs;

namespace Modules.Identity.Contracts.Services;

public interface IUserRoleService
{
    Task<string> AssignRoleAsync(string userId, List<UserRoleDto> userRoles, CancellationToken ct = default);
    
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken ct = default);
}