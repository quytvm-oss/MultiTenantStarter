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

namespace Modules.Billing.Features.v1.Invoices.VoidInvoice;

internal static class VoidInvoiceEndpoint
{
    public sealed record VoidInvoiceBody(string? Reason);

    internal static RouteHandlerBuilder MapVoidInvoiceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/invoices/{invoiceId:guid}/void",
                async (Guid invoiceId, VoidInvoiceBody? body, IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(await mediator.Send(new VoidInvoiceCommand(invoiceId, body?.Reason), ct)))
            .WithName("VoidInvoice")
            .WithSummary("Void an invoice")
            .RequirePermission(BillingPermissions.Manage);
    }
}
