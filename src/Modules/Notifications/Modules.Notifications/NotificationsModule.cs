using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

using Web.Modules;

namespace Modules.Notifications;

public class NotificationsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        throw new NotImplementedException();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        throw new NotImplementedException();
    }
}