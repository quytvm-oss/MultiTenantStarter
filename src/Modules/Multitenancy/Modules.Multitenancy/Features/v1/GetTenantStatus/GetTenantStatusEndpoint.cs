using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1.GetTenantStatus;

namespace Modules.Multitenancy.Features.v1.GetTenantStatus;

public static class GetTenantStatusEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id}/status",
                async (string id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(new GetTenantStatusQuery(id), cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("GetTenantStatus")
            .WithSummary("Get tenant status")
            .WithDescription("Retrieve status information for a tenant, including activation, validity, and basic metadata.")
            .Produces<TenantStatusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}