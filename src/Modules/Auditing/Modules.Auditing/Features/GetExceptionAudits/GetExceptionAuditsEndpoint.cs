using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Auditing.Contracts.Authorization;
using Modules.Auditing.Contracts.DTOs;
using Modules.Auditing.Contracts.v1.GetExceptionAudits;

using Shared.Identity.Authorization;

namespace Modules.Auditing.Features.GetExceptionAudits;

public static class GetExceptionAuditsEndpoint
{
    public static RouteHandlerBuilder MapGetExceptionAuditsEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("/exceptions",
                async ([AsParameters] GetExceptionAuditsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("GetExceptionAudits")
            .WithSummary("Get exception audit events")
            .WithDescription("Retrieve audit events related to exceptions.")
            .RequirePermission(AuditingPermissions.AuditTrails.View)
            .Produces<IEnumerable<AuditSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}