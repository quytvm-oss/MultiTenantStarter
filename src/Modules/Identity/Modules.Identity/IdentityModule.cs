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
using Modules.Identity.Features.v1.Tokens.RefreshToken;
using Modules.Identity.Features.v1.Tokens.TokenGeneration;
using Modules.Identity.Features.v1.Users.ChangePassword;
using Modules.Identity.Features.v1.Users.ConfirmEmail;
using Modules.Identity.Features.v1.Users.ForgotPassword;
using Modules.Identity.Features.v1.Users.GetUserById;
using Modules.Identity.Features.v1.Users.GetUserPermissions;
using Modules.Identity.Features.v1.Users.GetUserRoles;
using Modules.Identity.Features.v1.Users.GetUsers;
using Modules.Identity.Features.v1.Users.RegisterUser;
using Modules.Identity.Features.v1.Users.ResendConfirmationEmail;
using Modules.Identity.Features.v1.Users.ResetPassword;
using Modules.Identity.Services;

using Persistence;

using Shared.Identity;

using Storage;

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

        // users
        group.MapGetListUsersEndpoint();
        group.MapRegisterUserEndpoint();
        group.MapGetCurrentUserPermissionsEndpoint();
        group.MapGetUserRolesEndpoint();
        group.MapRegisterUserEndpoint();
        group.MapChangePasswordEndpoint();
        group.MapConfirmEmailEndpoint();
        group.MapForgotPasswordEndpoint();
        group.MapResetPasswordEndpoint();
        group.MapResendConfirmationEmailEndpoint();
        group.MapGetCurrentUserPermissionsEndpoint();
        group.MapGetUserByIdEndpoint();
    }
}