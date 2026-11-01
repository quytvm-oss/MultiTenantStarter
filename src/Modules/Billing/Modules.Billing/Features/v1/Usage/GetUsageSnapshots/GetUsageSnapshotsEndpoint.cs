using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Billing.Contracts.Authorization;
using Modules.Billing.Contracts.v1.Usage;

using Shared.Identity.Authorization;

namespace Modules.Billing.Features.v1.Usage.GetUsageSnapshots;

public static class GetUsageSnapshotsEndpoint
{
    internal static RouteHandlerBuilder MapGetUsageSnapshotsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/usage",
                (string? tenantId, int? periodYear, int? periodMonth, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetUsageSnapshotsQuery(tenantId, periodYear, periodMonth), ct))
            .WithName("GetUsageSnapshots")
            .WithSummary("List captured usage snapshots")
            .RequirePermission(BillingPermissions.View);
    }
}