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

namespace Modules.Billing.Features.v1.Wallets.ApproveTopupRequest;

internal static class ApproveTopupRequestEndpoint
{
    public sealed record ApproveTopupRequestBody(string? Note);

    internal static RouteHandlerBuilder MapApproveTopupRequestEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/wallet/topup-requests/{id:guid}/approve",
                async (Guid id, ApproveTopupRequestBody? body, IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(await mediator.Send(new ApproveTopupRequestCommand(id, body?.Note), ct)))
            .WithName("ApproveTopupRequest")
            .WithSummary("Approve a pending top-up request and issue the invoice")
            .RequirePermission(BillingPermissions.Manage)
            .WithIdempotency();
    }
}
