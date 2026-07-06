namespace Modules.Identity.Contracts.Services;

public interface IGroupRoleService
{
    Task<IReadOnlyList<string>> GetUserGroupRolesAsync(string userId, CancellationToken ct = default);
}