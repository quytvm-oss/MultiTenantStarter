using Asp.Versioning;

using Core.Context;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Identity.Authorization;
using Modules.Identity.Authorization.Jwt;
using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;
using Modules.Identity.Features.v1.Groups.AddUsersToGroup;
using Modules.Identity.Features.v1.Groups.CreateGroup;
using Modules.Identity.Features.v1.Groups.DeleteGroup;
using Modules.Identity.Features.v1.Groups.GetGroupById;
using Modules.Identity.Features.v1.Groups.GetGroups;
using Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;
using Modules.Identity.Features.v1.Groups.UpdateGroup;
using Modules.Identity.Features.v1.Impersonation.EndImpersonation;
using Modules.Identity.Features.v1.Impersonation.GetImpersonationGrants;
using Modules.Identity.Features.v1.Impersonation.RevokeImpersonationGrant;
using Modules.Identity.Features.v1.Impersonation.StartImpersonation;
using Modules.Identity.Features.v1.Permissions.GetPermissionCatalog;
using Modules.Identity.Features.v1.Roles.DeleteRole;
using Modules.Identity.Features.v1.Roles.GetRoleById;
using Modules.Identity.Features.v1.Roles.GetRoles;
using Modules.Identity.Features.v1.Roles.GetRoleWithPermissions;
using Modules.Identity.Features.v1.Roles.UpdateRolePermissions;
using Modules.Identity.Features.v1.Roles.UpsertRole;
using Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;
using Modules.Identity.Features.v1.Sessions.AdminRevokeSession;
using Modules.Identity.Features.v1.Sessions.GetMySessions;
using Modules.Identity.Features.v1.Sessions.GetTenantSessions;
using Modules.Identity.Features.v1.Sessions.GetUserSessions;
using Modules.Identity.Features.v1.Sessions.RevokeAllSessions;
using Modules.Identity.Features.v1.Sessions.RevokeSession;
using Modules.Identity.Features.v1.Tokens.RefreshToken;
using Modules.Identity.Features.v1.Tokens.TokenGeneration;
using Modules.Identity.Features.v1.TwoFactor.Disable;
using Modules.Identity.Features.v1.TwoFactor.Enroll;
using Modules.Identity.Features.v1.TwoFactor.VerifyEnroll;
using Modules.Identity.Features.v1.Users.AssignUserRoles;
using Modules.Identity.Features.v1.Users.ChangePassword;
using Modules.Identity.Features.v1.Users.ConfirmEmail;
using Modules.Identity.Features.v1.Users.DeleteUser;
using Modules.Identity.Features.v1.Users.ForgotPassword;
using Modules.Identity.Features.v1.Users.GetUserById;
using Modules.Identity.Features.v1.Users.GetUserGroups;
using Modules.Identity.Features.v1.Users.GetUserPermissions;
using Modules.Identity.Features.v1.Users.GetUserProfile;
using Modules.Identity.Features.v1.Users.GetUserRoles;
using Modules.Identity.Features.v1.Users.GetUsers;
using Modules.Identity.Features.v1.Users.RegisterUser;
using Modules.Identity.Features.v1.Users.ResendConfirmationEmail;
using Modules.Identity.Features.v1.Users.ResetPassword;
using Modules.Identity.Features.v1.Users.SearchUsers;
using Modules.Identity.Features.v1.Users.SelfRegistration;
using Modules.Identity.Features.v1.Users.SetProfileImage;
using Modules.Identity.Features.v1.Users.UpdateUser;
using Modules.Identity.Services;

using Persistence;

using Shared.Identity;

using Storage;

using Web.MessageBus;
using Web.Modules;

namespace Modules.Identity;

public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        PermissionConstants.Register(
            IdentityPermissions.All);

        var services = builder.Services;

        services.AddScoped<RolePermissionSyncer>();
        services.AddHostedService<RolePermissionSyncHostedService>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PathAwareAuthorizationHandler>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<ICurrentUserInitializer>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<IRequestContextService>());
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IImpersonationGrantService, ImpersonationGrantService>();

        // User services - focused single-responsibility services
        services.AddTransient<IUserRegistrationService, UserRegistrationService>();
        services.AddTransient<IUserProfileService, UserProfileService>();
        services.AddTransient<IUserStatusService, UserStatusService>();
        services.AddTransient<IUserRoleService, UserRoleService>();
        services.AddTransient<IUserPasswordService, UserPasswordService>();
        services.AddTransient<IUserPermissionService, UserPermissionService>();

        // Facade for backward compatibility
        services.AddTransient<IUserService, UserService>();

        services.AddTransient<IRoleService, RoleService>();
        services.AddCustomDbContext<IdentityDbContext>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddHeroStorage(builder.Configuration);
        services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>(
                name: "db:identity",
                failureStatus: HealthStatus.Unhealthy);
        services.AddScoped<IDbInitializer, IdentityDbInitializer>();

        // Configure password policy options
        services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));

        // Register password history service
        services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

        // Register password expiry service
        services.AddScoped<IPasswordExpiryService, PasswordExpiryService>();

        // Register session service and background cleanup
        services.AddScoped<ISessionService, SessionService>();

        // Register group role service for group-derived permissions
        services.AddScoped<IGroupRoleService, GroupRoleService>();


        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequiredLength = IdentityModuleConstants.PasswordLength;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.User.RequireUniqueEmail = true;

            // Account lockout: 5 consecutive failed logins → 15-minute lockout (applies to new users by default).
            // IdentityService's login flow drives AccessFailedAsync / IsLockedOutAsync.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        }).AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureJwtAuth();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/identity")
            .WithTags("Identity")
            .WithApiVersionSet(apiVersionSet);

        // tokens
        group.MapGenerateTokenEndpoint().AllowAnonymous().RequireRateLimiting("auth");
        group.MapRefreshTokenEndpoint().AllowAnonymous().RequireRateLimiting("auth");

        // permission catalog — every permission registered with the host,
        // filtered to the caller's tenant context (root vs admin set)
        group.MapGetPermissionCatalogEndpoint();

        // users
        group.MapGetListUsersEndpoint();
        group.MapRegisterUserEndpoint();
        group.MapGetCurrentUserPermissionsEndpoint();
        group.MapGetUserRolesEndpoint();
        group.MapChangePasswordEndpoint();
        group.MapConfirmEmailEndpoint();
        group.MapForgotPasswordEndpoint();
        group.MapResetPasswordEndpoint();
        group.MapResendConfirmationEmailEndpoint();
        group.MapGetUserByIdEndpoint();
        group.MapAssignUserRolesEndpoint();
        group.MapDeleteUserEndpoint();
        group.MapSelfRegisterUserEndpoint();
        group.MapGetMeEndpoint();
        group.MapUpdateUserEndpoint();
        group.MapSetProfileImageEndpoint();
        group.MapGetUserGroupsEndpoint();
        group.MapSearchUsersEndpoint();

        // sessions - user endpoints
        group.MapGetMySessionsEndpoint();
        group.MapGetUserSessionsEndpoint();
        group.MapRevokeAllSessionsEndpoint();
        group.MapRevokeSessionEndpoint();
        group.MapGetTenantSessionsEndpoint();
        group.MapAdminRevokeAllSessionsEndpoint();
        group.MapAdminRevokeSessionEndpoint();

        //roles
        group.MapGetRolesQuery();
        group.MapGetRoleByIdEndpoint();
        group.MapGetRoleWithPermissionsEndpoint();
        group.MapUpdateRolePermissionsEndpoint();
        group.MapCreateOrUpdateRoleEndpoint();
        group.MapDeleteRoleEndpoint();

        // groups
        group.MapCreateGroupEndpoint();
        group.MapAddUsersToGroupEndpoint();
        group.MapDeleteGroupEndpoint();
        group.MapGetGroupByIdEndpoint();
        group.MapGetGroupsEndpoint();
        group.MapRemoveUserFromGroupEndpoint();
        group.MapUpdateGroupEndpoint();

        // impersonal grant
        group.MapStartImpersonationEndpoint();
        group.MapEndImpersonationEndpoint();
        group.MapRevokeImpersonationGrantEndpoint();
        group.MapGetImpersonationGrantsEndpoint();

        // two factor
        group.MapEnrollTwoFactorEndpoint();
        group.MapVerifyEnrollTwoFactorEndpoint();
        group.MapDisableTwoFactorEndpoint();
    }
}