using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Groups.DeleteGroup;
using Modules.Identity.Data;

namespace Modules.Identity.Features.v1.Groups.DeleteGroup;

public class DeleteGroupCommandHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IUserPermissionService userPermissionService)
    : ICommandHandler<DeleteGroupCommand>
{
    
    public async ValueTask<Unit> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == command.Id,cancellationToken)
            ?? throw new NotFoundException($"Group with ID '{command.Id}' not found");

        if (group.IsSystemGroup)
        {
            throw new ForbiddenException("System groups cannot be deleted.");
        }
        
        // Snapshot members before delete; soft-delete flips IsDeleted but membership rows
        // persist, so capture first for clarity.
        var memberIds = await dbContext.UserGroups
            .Where(ug => ug.GroupId == group.Id)
            .Select(ug => ug.UserId)
            .ToListAsync(cancellationToken);
        
        // Soft delete via domain method
        group.Delete(currentUser.GetUserId().ToString());
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // A deleted group can no longer contribute its roles to members' effective
        // permission sets — flush each member's cached entry.
        foreach (var userId in memberIds)
        {
            await userPermissionService.InvalidatePermissionCacheAsync(userId, cancellationToken)
                .ConfigureAwait(false);
        }
        
        return Unit.Value;
    }
}