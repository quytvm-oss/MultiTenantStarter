using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using Modules.Identity.Data;

namespace Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public class RemoveUserFromGroupCommandHandler(
    IdentityDbContext dbContext,
    IUserPermissionService userPermissionService)
    : ICommandHandler<RemoveUserFromGroupCommand>
{
  
    public async ValueTask<Unit> Handle(RemoveUserFromGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        var membership = await dbContext.UserGroups
            .Include(ug => ug.Group)
            .FirstOrDefaultAsync(x => x.UserId == command.UserId && x.GroupId == command.GroupId, cancellationToken)
            ?? throw new NotFoundException($"User '{command.UserId}'  with group '{command.GroupId}' not found.");
        
        // Default groups (e.g. seeded "All Users") require every tenant user to be a member, so
        // removing one breaks that invariant and leaves later registrants in a half-populated group.
        if (membership.Group is not null && membership.Group.IsDefault)
        {
            throw new ForbiddenException("Users cannot be removed from a default group.");
        }
        
        dbContext.UserGroups.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Leaving a group may revoke roles the user only held through this group —
        // invalidate so the cached permission set is rebuilt on next request.
        await userPermissionService.InvalidatePermissionCacheAsync(command.UserId, cancellationToken)
            .ConfigureAwait(false);
        
        return Unit.Value;
    }
}