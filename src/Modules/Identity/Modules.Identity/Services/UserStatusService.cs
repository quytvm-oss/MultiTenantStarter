using System.Net;

using Core.Context;
using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Modules.Auditing.Contracts;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Domain;

using Shared.Identity;
using Shared.Multitenancy;

namespace Modules.Identity.Services;

public sealed class UserStatusService(
    UserManager<User> userManager,
    IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor,
    ICurrentUser currentUser,
    IAuditClient auditClient)
    : IUserStatusService
{
    public async Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantContextAccessor.MultiTenantContext.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
        
        var context = await BuildToggleContextAsync(userId, activateUser, ct);
        
        await ValidateTogglePermissionsAsync(context, ct);
        
        ApplyStatusChange(context);
        
        var result = await userManager.UpdateAsync(context.TargetUser);
        if (!result.Succeeded)
        {
            throw new CustomException("Toggle status failed", result.Errors.Select(e => e.Description).ToList(), HttpStatusCode.BadRequest);
        }
        
        await auditClient.WriteActivityAsync(
            ActivityKind.Command,
            name: "ToggleUserStatus",
            statusCode: 204,
            durationMs: 0,
            captured: BodyCapture.None,
            requestSize: 0,
            responseSize: 0,
            requestPreview: new { actorId = context.ActorId.ToString(), targetUserId = context.TargetUser.Id, action = context.ActivateUser ? "activate" : "deactivate", tenant = context.TenantId ?? "unknown" },
            responsePreview: new { outcome = "success" },
            severity: AuditSeverity.Information,
            source: "Identity",
            ct: ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(string userId, CancellationToken ct = default)
        => ToggleStatusAsync(activateUser: false, userId, ct);

    #region internals

    private async Task<ToggleStatusContext> BuildToggleContextAsync(string userId, bool activateUser,
        CancellationToken ct = default)
    {
        var actorId = currentUser.GetUserId();
        if (actorId == Guid.Empty)
        {
            throw new UnauthorizedException("authenticated user required to toggle status");
        }
        
        var actor = await userManager.FindByIdAsync(actorId.ToString())
            ?? throw new NotFoundException("authenticated user not found");

        var targetUser = await userManager.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken: ct) 
            ?? throw new NotFoundException("user not found");
        
        return new ToggleStatusContext(
            ActorId: actorId,
            Actor: actor,
            TargetUser: targetUser,
            ActivateUser: activateUser,
            TenantId: tenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id);
    }

    private async Task ValidateTogglePermissionsAsync(ToggleStatusContext context, CancellationToken ct = default)
    {
        if (!await userManager.IsInRoleAsync(context.Actor, RoleConstants.Admin))
        {
            await AuditPolicyFailureAsync(context, "ActorNotAdmin", ct);
            throw new ForbiddenException("Only administrators can change user status.");
        }

        if (!context.ActivateUser && context.ActorId.ToString() == context.TargetUser.Id)
        {
            await AuditPolicyFailureAsync(context, "SelfDeactivationBlocked", ct);
            throw new CustomException("Users cannot deactivate themselves.", Array.Empty<string>(), HttpStatusCode.BadRequest);
        }
        
        if (!context.ActivateUser && await userManager.IsInRoleAsync(context.TargetUser, RoleConstants.Admin))
        {
            await AuditPolicyFailureAsync(context, "AdminDeactivationBlocked", ct);
            throw new CustomException("Administrators cannot be deactivated.", Array.Empty<string>(), HttpStatusCode.BadRequest);
        }
        
        if (!context.ActivateUser)
        {
            var activeAdmins = await userManager.GetUsersInRoleAsync(RoleConstants.Admin);
            if (!activeAdmins.Any(u => u.IsActive))
            {
                await AuditPolicyFailureAsync(context, "NoActiveAdmins", ct);
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
    
    private async Task AuditPolicyFailureAsync(
        ToggleStatusContext context,
        string reason,
        CancellationToken cancellationToken)
    {
        var claims = new Dictionary<string, object?>
        {
            ["actorId"] = context.ActorId.ToString(),
            ["targetUserId"] = context.TargetUser.Id,
            ["tenant"] = context.TenantId ?? "unknown",
            ["action"] = context.ActivateUser ? "activate" : "deactivate"
        };

        await auditClient.WriteSecurityAsync(
            SecurityAction.PolicyFailed,
            subjectId: context.ActorId.ToString(),
            reasonCode: reason,
            claims: claims,
            severity: AuditSeverity.Warning,
            source: "Identity",
            ct: cancellationToken).ConfigureAwait(false);
    }


    private sealed record ToggleStatusContext(
        Guid ActorId,
        User Actor,
        User TargetUser,
        bool ActivateUser,
        string? TenantId);

    #endregion
}