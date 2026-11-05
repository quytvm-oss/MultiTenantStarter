using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Multitenancy.Contracts.Authorization;
using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1.GetTenantTheme;

using Shared.Identity.Authorization;

namespace Modules.Multitenancy.Features.v1.GetTenantTheme;

internal sealed class GetTenantThemeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/theme", async (IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(new GetTenantThemeQuery(), cancellationToken)))
            .WithName("GetTenantTheme")
            .WithSummary("Get current tenant theme")
            .WithDescription("Retrieve the theme settings for the current tenant, including colors, typography, and brand assets.")
            .RequirePermission(MultitenancyPermissions.Tenants.ViewTheme)
            .Produces<TenantThemeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
