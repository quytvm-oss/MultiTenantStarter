using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Groups.GetGroupById;
using Modules.Identity.Data;

namespace Modules.Identity.Features.v1.Groups.GetGroupById;

public class GetGroupByIdQueryHandler(IdentityDbContext dbContext) : IQueryHandler<GetGroupByIdQuery, GroupDto>
{
    
    public async ValueTask<GroupDto> Handle(GetGroupByIdQuery query, CancellationToken cancellationToken)
    {
        var group = await dbContext.Groups
            .AsNoTracking().Include(x => x.GroupRoles)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{query.Id}'  not found.");

        var memberCount = await dbContext.UserGroups
            .AsNoTracking()
            .Where(x => x.GroupId == group.Id).CountAsync(cancellationToken);
        
        var roleIds = group.GroupRoles.Select(x => x.RoleId).ToList();
        var roleNames = roleIds.Count > 0
            ? await dbContext.Roles.AsNoTracking()
                .Where(x => roleIds.Contains(x.Id))
                .Select(x => x.Name!).ToListAsync(cancellationToken)
            : [];

        return new GroupDto()
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsDefault = group.IsDefault,
            IsSystemGroup = group.IsSystemGroup,
            MemberCount = memberCount,
            RoleIds = roleIds.AsReadOnly(),
            RoleNames = roleNames.AsReadOnly(),
            CreatedAt = group.CreatedOnUtc
        };
    }
}