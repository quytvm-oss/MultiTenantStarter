using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Billing.Contracts.Authorization;

using Modules.Billing.Contracts.v1.Plans;

using Shared.Identity.Authorization;

using Web.Idempotency;

namespace Modules.Billing.Features.v1.Plans.CreatePlan;

public static class CreatePlanEndpoint
{
    public static RouteHandlerBuilder MapCreatePlanEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/plans",
                async (CreatePlanCommand command, IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(await mediator.Send(command, ct)))
            .WithName("CreateBillingPlan")
            .WithSummary("Create a new billing plan")
            .RequirePermission(BillingPermissions.Manage)
            .WithIdempotency();
    }
}
