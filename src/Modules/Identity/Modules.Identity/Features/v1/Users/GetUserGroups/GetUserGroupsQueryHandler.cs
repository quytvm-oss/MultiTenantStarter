using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.GetUserGroups;
using Modules.Identity.Data;

namespace Modules.Identity.Features.v1.Users.GetUserGroups;

public class GetUserGroupsQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetUserGroupsQuery, IEnumerable<GroupDto>>
{
   
    public async ValueTask<IEnumerable<GroupDto>> Handle(GetUserGroupsQuery query, CancellationToken cancellationToken)
    {
        // validate user
        var userExists = await  dbContext.Users.AsNoTracking()
            .AnyAsync(x => x.Id == query.UserId, cancellationToken);
        
        if (!userExists)
            throw new NotFoundException($"User with ID '{query.UserId}' not found.");
        
        // get user's group
        var groupIds = await dbContext.UserGroups.AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Select(x => x.GroupId)
            .ToListAsync(cancellationToken);

        if (!groupIds.Any())
            return [];

        var groups = await dbContext.Groups.AsNoTracking()
            .Include(x => x.GroupRoles)
            .Where(group => groupIds.Contains(group.Id))
            .ToListAsync(cancellationToken);
        
        // Get member counts
        var membersCount = await dbContext.UserGroups
            .AsNoTracking()
            .Where(ug => groupIds.Contains(ug.GroupId))
            .GroupBy(ug => ug.GroupId)
            .Select(g => new {  GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId,x => x.Count, cancellationToken);
        
        // Get role names
        var allRoleIds = groups
            .SelectMany(g => g.GroupRoles.Select(gr => gr.RoleId))
            .Distinct()
            .ToList();

        var roleNames = allRoleIds.Count > 0
            ? await dbContext.Roles.AsNoTracking()
                .Where(x => allRoleIds.Contains(x.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name!, cancellationToken)
            : new Dictionary<string, string>();

        return groups.Select(g => new GroupDto()
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            IsDefault = g.IsDefault,
            IsSystemGroup = g.IsSystemGroup,
            MemberCount = membersCount.GetValueOrDefault(g.Id, 0),
            RoleIds = g.GroupRoles.Select(gr => gr.RoleId).ToList().AsReadOnly(),
            RoleNames = g.GroupRoles
                .Select(gr => roleNames.GetValueOrDefault(gr.RoleId, gr.RoleId))
                .ToList()
                .AsReadOnly(),
            CreatedAt = g.CreatedOnUtc
        });
    }
}