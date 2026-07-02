using Core.Context;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Identity.Authorization.Jwt;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;
using Modules.Identity.Services;

using Persistence;

using Web.Modules;

namespace Modules.Identity;

public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<ICurrentUserInitializer>(sp => sp.GetRequiredService<ICurrentUserService>());
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<IRequestContextService>());
        
        services.AddScoped<ITokenService, TokenService>();
        
        // User services - focused single-responsibility services
        services.AddTransient<IUserPermissionService, UserPermissionService>();
        
        // Facade for backward compatibility
        services.AddTransient<IUserService, UserService>();
        
        services.AddCustomDbContext<IdentityDbContext>();
        services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>(
                name: "db:identity",
                failureStatus: HealthStatus.Unhealthy);
        services.AddScoped<IDbInitializer, IdentityDbInitializer>();

        services.Configure<PasswordPolicyOptions>(builder.Configuration.GetSection("PasswordPolicy"));


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

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
       
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
       
    }
}