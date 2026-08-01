using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Groups.AddUsersToGroup;
using Modules.Identity.Data;
using Modules.Identity.Domain;

namespace Modules.Identity.Features.v1.Groups.AddUsersToGroup;

public class AddUsersToGroupCommandHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IUserPermissionService userPermissionService)
    : ICommandHandler<AddUsersToGroupCommand, AddUsersToGroupResponse>
{

    public async ValueTask<AddUsersToGroupResponse> Handle(AddUsersToGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        // Validate group exists
        var groupExists = await dbContext.Groups
            .AnyAsync(x => x.Id == command.GroupId, cancellationToken);
        
        if (!groupExists)
            throw new NotFoundException($"Group with id {command.GroupId} does not exist");
        
        // Validate user IDs user
        var existingUserIds = await dbContext.Users
            .Where(u => command.UserIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var invalidUserIds = command.UserIds.Except(existingUserIds).ToList();
        if (invalidUserIds.Count > 0)
            throw new NotFoundException($"Users not found:  {string.Join(",", invalidUserIds)}");
        
        // Get existing memberships
        var existingMemberships = await dbContext.UserGroups
            .Where(ug => ug.GroupId == command.GroupId && command.UserIds.Contains(ug.UserId))
            .Select(ug => ug.UserId)
            .ToListAsync(cancellationToken);

        var alreadyMemberUserIds = existingMemberships.ToList();
        var userToAdd = command.UserIds.Except(existingUserIds).ToList();
        
        // Add new member
        var currentUserId = currentUser.GetUserId().ToString();
        foreach (var userId in userToAdd)
        {
           await  dbContext.UserGroups.AddAsync(UserGroup.Create(userId, command.GroupId, currentUserId), 
               cancellationToken);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Joining a group can grant new roles (via GroupRoles) feeding JWT claims; invalidate
        // each newly-added user's cached permission set so their next request reflects it.
        foreach (var userId in userToAdd)
        {
            await userPermissionService.InvalidatePermissionCacheAsync(userId, cancellationToken)
                .ConfigureAwait(false);
        }

        return new AddUsersToGroupResponse(userToAdd.Count, alreadyMemberUserIds);
    }
}