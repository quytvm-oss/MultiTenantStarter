using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Billing.Contracts.Authorization;
using Modules.Billing.Contracts.v1.Wallets;

using Shared.Identity.Authorization;

using Web.Idempotency;

namespace Modules.Billing.Features.v1.Wallets.RejectTopupRequest;

internal static class RejectTopupRequestEndpoint
{
    public sealed record RejectTopupRequestBody(string? Reason);

    internal static RouteHandlerBuilder MapRejectTopupRequestEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/wallet/topup-requests/{id:guid}/reject",
                async (Guid id, RejectTopupRequestBody? body, IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(await mediator.Send(new RejectTopupRequestCommand(id, body?.Reason), ct)))
            .WithName("RejectTopupRequest")
            .WithSummary("Reject a pending top-up request")
            .RequirePermission(BillingPermissions.Manage)
            .WithIdempotency();
    }
}
