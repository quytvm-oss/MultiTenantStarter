using System.Net;

using Core.Context;
using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Domain;

using Shared.Identity;
using Shared.Multitenancy;

namespace Modules.Identity.Services;

public sealed class UserStatusService : IUserStatusService
{
    private readonly UserManager<User> _userManager;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantContextAccessor;
    private readonly ICurrentUser _currentUser;

    public UserStatusService(UserManager<User> userManager, IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor, ICurrentUser currentUser)
    {
        _userManager = userManager;
        _tenantContextAccessor = tenantContextAccessor;
        _currentUser = currentUser;
    }

    public async Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_tenantContextAccessor.MultiTenantContext.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
        
        var context = await BuildToggleContextAsync(userId, activateUser, ct);
        
        await ValidateTogglePermissionsAsync(context, ct);
        
        ApplyStatusChange(context);
        
        var result = await _userManager.UpdateAsync(context.TargetUser);
        if (!result.Succeeded)
        {
            throw new CustomException("Toggle status failed", result.Errors.Select(e => e.Description).ToList(), HttpStatusCode.BadRequest);
        }
    }

    public Task DeleteAsync(string userId, CancellationToken ct = default)
        => ToggleStatusAsync(activateUser: false, userId, ct);

    #region internals

    private async Task<ToggleStatusContext> BuildToggleContextAsync(string userId, bool activateUser,
        CancellationToken ct = default)
    {
        var actorId = _currentUser.GetUserId();
        if (actorId == Guid.Empty)
        {
            throw new UnauthorizedException("authenticated user required to toggle status");
        }
        
        var actor = await _userManager.FindByIdAsync(actorId.ToString())
            ?? throw new NotFoundException("authenticated user not found");

        var targetUser = await _userManager.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken: ct) 
            ?? throw new NotFoundException("user not found");
        
        return new ToggleStatusContext(
            ActorId: actorId,
            Actor: actor,
            TargetUser: targetUser,
            ActivateUser: activateUser,
            TenantId: _tenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id);
    }

    private async Task ValidateTogglePermissionsAsync(ToggleStatusContext context, CancellationToken ct = default)
    {
        if (!await _userManager.IsInRoleAsync(context.Actor, RoleConstants.Admin))
        {
            throw new ForbiddenException("Only administrators can change user status.");
        }

        if (!context.ActivateUser && context.ActorId.ToString() == context.TargetUser.Id)
        {
            throw new CustomException("Users cannot deactivate themselves.", Array.Empty<string>(), HttpStatusCode.BadRequest);
        }
        
        if (!context.ActivateUser && await _userManager.IsInRoleAsync(context.TargetUser, RoleConstants.Admin))
        {
            throw new CustomException("Administrators cannot be deactivated.", Array.Empty<string>(), HttpStatusCode.BadRequest);
        }
        
        if (!context.ActivateUser)
        {
            var activeAdmins = await _userManager.GetUsersInRoleAsync(RoleConstants.Admin);
            if (!activeAdmins.Any(u => u.IsActive))
            {
                throw new CustomException("Tenant must have at least one active administrator.", Array.Empty<string>(), HttpStatusCode.BadRequest);
            }
        }
    }
    
    
    private static void ApplyStatusChange(ToggleStatusContext context)
    {
        if (context.ActivateUser)
        {
            context.TargetUser.Activate(context.ActorId.ToString(), context.TenantId);
        }
        else
        {
            context.TargetUser.Deactivate(context.ActorId.ToString(), "Status toggled by administrator", context.TenantId);
        }
    }


    private sealed record ToggleStatusContext(
        Guid ActorId,
        User Actor,
        User TargetUser,
        bool ActivateUser,
        string? TenantId);

    #endregion
}