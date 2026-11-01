using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Billing.Contracts.Authorization;
using Modules.Billing.Contracts.v1.Invoices;

using Shared.Identity.Authorization;

using Web.Idempotency;

namespace Modules.Billing.Features.v1.Invoices.IssueInvoice;

internal static class IssueInvoiceEndpoint
{
    public sealed record IssueInvoiceBody(DateTime? DueAtUtc);

    internal static RouteHandlerBuilder MapIssueInvoiceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/invoices/{invoiceId:guid}/issue",
                async (Guid invoiceId, IssueInvoiceBody? body, IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(await mediator.Send(new IssueInvoiceCommand(invoiceId, body?.DueAtUtc), ct)))
            .WithName("IssueInvoice")
            .WithSummary("Issue a draft invoice")
            .RequirePermission(BillingPermissions.Manage)
            .WithIdempotency();
    }
}
