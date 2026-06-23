using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1.GetTenants;

using Shared.Persistence;

namespace Modules.Multitenancy.Features.v1.GetTenants;

public static class GetTenantsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
                "/",
                async ([AsParameters] GetTenantsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("ListTenants")
            .WithSummary("List tenants")
            .WithDescription("Retrieve tenants for the current environment with pagination and optional sorting.")
            .Produces<PagedResponse<TenantDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}