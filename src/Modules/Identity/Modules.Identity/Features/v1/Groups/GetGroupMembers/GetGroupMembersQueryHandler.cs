using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Groups.GetGroupMembers;
using Modules.Identity.Data;

namespace Modules.Identity.Features.v1.Groups.GetGroupMembers;

public class GetGroupMembersQueryHandler(IdentityDbContext dbContext)
    : IQueryHandler<GetGroupMembersQuery, IEnumerable<GroupMemberDto>>
{
    
    public async ValueTask<IEnumerable<GroupMemberDto>> Handle(GetGroupMembersQuery query, CancellationToken cancellationToken)
    {
        // validate group exist
        var groupExist = await dbContext.Groups.AsNoTracking()
            .AnyAsync(g => g.Id == query.GroupId, cancellationToken);
        
        if (!groupExist)
            throw new NotFoundException($"Group with id {query.GroupId} does not exist");

        var memberShips = await dbContext.UserGroups.AsNoTracking()
            .Where(ug => ug.GroupId == query.GroupId)
            .Join(dbContext.Users ,
                ug => ug.UserId,
                u => u.Id,
                (ug, u) => new GroupMemberDto()
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AddedAt = ug.AddedAt,
                    AddedBy = ug.AddedBy
                })
            .OrderBy(x => x.UserName)
            .ToListAsync(cancellationToken);
        
        return memberShips;
    }
}