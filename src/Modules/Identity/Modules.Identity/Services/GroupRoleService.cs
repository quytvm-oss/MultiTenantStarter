using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;

namespace Modules.Identity.Services;

public class GroupRoleService : IGroupRoleService
{
    private readonly IdentityDbContext _dbContext;

    public GroupRoleService(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IReadOnlyList<string>> GetUserGroupRolesAsync(string userId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var userGroupIds = await _dbContext.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync(ct);

        if (userGroupIds.Count == 0)
            return [];
        
        var groupRoles = await _dbContext.GroupRoles
            .Where(gr => userGroupIds.Contains(gr.GroupId))
            .Select(gr => gr.Role!.Name!)
            .Distinct()
            .ToListAsync(ct);
        
        return groupRoles;
    }
}