using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Billing.Contracts;
using Modules.Billing.Contracts.v1.Invoices;

namespace Modules.Billing.Features.v1.Invoices.GetMyInvoices;

internal static class GetMyInvoicesEndpoint
{
    internal static RouteHandlerBuilder MapGetMyInvoicesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/invoices/me",
                (InvoiceStatus? status, int? periodYear, int? periodMonth,
                 int pageNumber, int pageSize, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetMyInvoicesQuery(
                        status,
                        periodYear,
                        periodMonth,
                        pageNumber <= 0 ? 1 : pageNumber,
                        pageSize <= 0 ? 20 : Math.Min(pageSize, 100)), ct))
            .WithName("GetMyInvoices")
            .WithSummary("List invoices for the current tenant");
    }
}
