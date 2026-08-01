using System.Net;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Contracts.v1.Groups.UpdateGroup;
using Modules.Identity.Data;
using Modules.Identity.Domain;

namespace Modules.Identity.Features.v1.Groups.UpdateGroup;

public class UpdateGroupCommandHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IUserPermissionService userPermissionService)
    : ICommandHandler<UpdateGroupCommand, GroupDto>
{

    public async ValueTask<GroupDto> Handle(UpdateGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        var group = await GetGroupAsync(command.Id, cancellationToken);
        
        // System groups are framework-managed — name, description, default flag, and role
        // assignments are all part of the seed contract that the startup syncer relies on.
        if (group.IsSystemGroup)
        {
            throw new ForbiddenException($"System groups cannot be modified..");
        }
        
        await ValidateUniqueNameAsync(command.Id, command.Name, cancellationToken);
        await ValidateRoleIdsAsync(command.RoleIds, cancellationToken);
        
        var userId = currentUser.GetUserId().ToString();
        group.Update(command.Name, command.Description, userId);
        group.SetAsDefault(command.IsDefault);
        
        var currentRoleIdsBefore = group.GroupRoles.Select(gr => gr.RoleId).ToHashSet();
        var newRoleIds = UpdateRoleAssignments(group, command.RoleIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // If the set of group→role assignments actually changed, every member's
        // effective permission set may have shifted — invalidate each.
        if (!currentRoleIdsBefore.SetEquals(newRoleIds))
        {
            var memberIds = await dbContext.UserGroups
                .Where(ug => ug.GroupId == group.Id)
                .Select(ug => ug.UserId).ToListAsync(cancellationToken);

            foreach (var member in memberIds)
            {
                await userPermissionService.InvalidatePermissionCacheAsync(member, cancellationToken)
                    .ConfigureAwait(false);;
            }
        }
        
        return await BuildResponseAsync(group, newRoleIds, cancellationToken);
    }

    private async Task<GroupDto> BuildResponseAsync(Group group, HashSet<string> roleIds, CancellationToken cancellationToken)
    {
        var memberCount = await dbContext.UserGroups.AsNoTracking()
            .CountAsync(ug => ug.GroupId == group.Id, cancellationToken);

        var roleNames = roleIds.Count > 0
            ? await dbContext.Roles
                .Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!)
                .ToListAsync(cancellationToken)
            : [];

        return new GroupDto()
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsDefault = group.IsDefault,
            IsSystemGroup = group.IsSystemGroup,
            MemberCount = memberCount,
            RoleNames = roleNames.AsReadOnly(),
            RoleIds = roleIds.ToList().AsReadOnly(),
            CreatedAt = group.CreatedOnUtc
        };
    }

    private HashSet<string> UpdateRoleAssignments(Group group, IReadOnlyList<string>? roleIds)
    {
        var currentRoleIds = group.GroupRoles.Select(gr => gr.RoleId).ToHashSet();
        var newRoleIds = roleIds?.ToHashSet() ?? [];
        
        var rolesToRemove = group.GroupRoles.Where(gr => !newRoleIds.Contains(gr.RoleId)).ToList();

        foreach (var roleId in rolesToRemove)
        {
            group.GroupRoles.Remove(roleId);
        }

        foreach (var roleId in newRoleIds.Where(id => !currentRoleIds.Contains(id)))
        {
            group.GroupRoles.Add(GroupRole.Create(group.Id, roleId));
        }

        return newRoleIds;
    }

    private async Task ValidateRoleIdsAsync(IReadOnlyList<string>? roledIds, CancellationToken cancellationToken)
    {
        if (roledIds is not { Count: > 0 })
        {
            return;
        }
        
        var existingRoleIds = await dbContext.Roles
            .Where(r => roledIds.Contains(r.Id))
            .Select(r => r.Id) 
            .ToListAsync(cancellationToken);
        
        var invalidRoleIds = roledIds.Except(existingRoleIds).ToList();
        if (invalidRoleIds.Count > 0)
        {
            throw new CustomException($"Roles not found: {string.Join(", ", invalidRoleIds)}");
        }
    }

    private async Task ValidateUniqueNameAsync(Guid commandId, string commandName, CancellationToken cancellationToken)
    {
        var nameExists = await dbContext.Groups
            .AnyAsync(x => x.Name == commandName && x.Id != commandId, cancellationToken);
        if (nameExists)
        {
            throw new CustomException($"Group with name '{commandName}' already exists.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }
    }

    private async Task<Group> GetGroupAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await dbContext.Groups
                        .AsNoTracking().Include(x => x.GroupRoles)
                        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new NotFoundException($"Group with ID '{id}'  not found.");
        return group;
    }
}