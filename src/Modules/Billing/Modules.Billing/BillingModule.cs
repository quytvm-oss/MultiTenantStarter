using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

using Web.Modules;

namespace Modules.Billing;

public class BillingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder, bool IsWebHost = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}