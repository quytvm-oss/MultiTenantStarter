using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Auditing.Contracts.Authorization;
using Modules.Auditing.Contracts.DTOs;
using Modules.Auditing.Contracts.v1.GetAuditsByCorrelation;

using Shared.Identity.Authorization;

namespace Modules.Auditing.Features.GetAuditsByCorrelation;

public static class GetAuditsByCorrelationEndpoint
{
    public static RouteHandlerBuilder MapGetAuditsByCorrelationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
            "/by-correlation/{correlationId}",
            async (string correlationId, DateTime? fromUtc, DateTime? toUtc, IMediator mediator, CancellationToken ct)
                => TypedResults.Ok(await mediator.Send(
                    new GetAuditsByCorrelationQuery()
                    {
                        CorrelationId = correlationId, FromUtc = fromUtc, ToUtc = toUtc,
                    }, ct)))
            .WithName("GetAuditsByCorrelation")
            .WithSummary("Get audit events by correlation id")
            .WithDescription("Retrieve audit events associated with a given correlation id.")
            .RequirePermission(AuditingPermissions.AuditTrails.View)
            .Produces<IEnumerable<AuditSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}