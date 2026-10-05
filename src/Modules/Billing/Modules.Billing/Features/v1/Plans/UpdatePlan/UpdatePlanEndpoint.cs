using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Billing.Contracts.Authorization;

using Modules.Billing.Contracts.v1.Plans;

using Shared.Identity.Authorization;

namespace Modules.Billing.Features.v1.Plans.UpdatePlan;

public static class UpdatePlanEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePlanEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/plans/{planId:guid}",
                async (Guid planId, UpdatePlanCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { PlanId = planId };
                    return TypedResults.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdateBillingPlan")
            .WithSummary("Update a billing plan")
            .RequirePermission(BillingPermissions.Manage);
    }
}
