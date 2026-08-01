using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Groups.GetGroups;
using Modules.Identity.Data;

namespace Modules.Identity.Features.v1.Groups.GetGroups;

public class GetGroupsQueryHandler(IdentityDbContext dbContext) : IQueryHandler<GetGroupsQuery, IEnumerable<GroupDto>>
{
   
    public async ValueTask<IEnumerable<GroupDto>> Handle(GetGroupsQuery query, CancellationToken cancellationToken)
    {
        var groupsQuery = dbContext.Groups.AsNoTracking()
            .Include(x => x.GroupRoles)
            .AsQueryable();
        
        // Apply search filter
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLowerInvariant();
            groupsQuery = groupsQuery.Where(x => 
                x.Name.ToLower().Contains(searchTerm) ||
             ( x.Description != null && x.Description.ToLower().Contains(searchTerm)));
        }

        var groups = await groupsQuery
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        
        // Get member counts in one query
        var groupIds = groups.Select(x => x.Id).ToList();
        var memberCounts = await dbContext.UserGroups
            .AsNoTracking()
            .Where(ug => groupIds.Contains(ug.GroupId))
            .GroupBy(ug => ug.GroupId)
            .Select(ug => new { GroupId = ug.Key, Count = ug.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
        
        // Get all role IDs from groups
        var allRoleIds = groups
            .SelectMany(x => x.GroupRoles.Select(g => g.RoleId))
            .Distinct().ToList();
        
        var roleNames = await dbContext.Roles
            .AsNoTracking().Where(r => allRoleIds.Contains(r.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name!,cancellationToken);

        return groups.Select(x => new GroupDto()
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsDefault = x.IsDefault,
            IsSystemGroup = x.IsSystemGroup,
            MemberCount = memberCounts.GetValueOrDefault(x.Id, 0),
            RoleIds = x.GroupRoles.Select(g => g.RoleId).ToList().AsReadOnly(),
            RoleNames = x.GroupRoles
                .Select(gr => roleNames.GetValueOrDefault(gr.RoleId, gr.RoleId))
                .ToList().AsReadOnly(),
            CreatedAt = x.CreatedOnUtc
        });
    }
}