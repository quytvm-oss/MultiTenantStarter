using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Web.Modules;

public interface IModule
{
    void ConfigureServices(IHostApplicationBuilder builder);
    
    void ConfigureMiddleware(IApplicationBuilder app);
    
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}