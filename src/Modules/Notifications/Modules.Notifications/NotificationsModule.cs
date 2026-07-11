using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

using Web.Modules;

namespace Modules.Notifications;

public class NotificationsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}