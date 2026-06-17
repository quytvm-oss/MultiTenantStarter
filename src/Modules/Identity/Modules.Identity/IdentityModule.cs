using Core.Context;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Web.Modules;

namespace Modules.Identity;

public class IdentityModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ICurrentUser, NoCurrentUser>();
        builder.Services.AddScoped<ICurrentUserInitializer, NoCurrentUserInitializer>();
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
       
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
       
    }
}