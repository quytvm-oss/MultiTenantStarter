using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Permissions.GetPermissionCatalog;

using Shared.Identity.Authorization;

namespace Modules.Identity.Features.v1.Permissions.GetPermissionCatalog;

public static class GetPermissionCatalogEndpoint
{
    internal static RouteHandlerBuilder MapGetPermissionCatalogEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/permissions/catalog", async (IMediator mediator, CancellationToken ct)=>
            TypedResults.Ok( await mediator.Send(new GetPermissionCatalogQuery(),ct)))
            .WithName("GetPermissionCatalog")
            .WithSummary("Get permission catalog")
            .RequirePermission(IdentityPermissions.Roles.View)
            .WithDescription("Returns every permission registered in the host, filtered to the caller's tenant context. Non-root tenants see the Admin set; the root tenant additionally sees the platform Root set.")
            .Produces<IReadOnlyList<PermissionCatalogEntryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}