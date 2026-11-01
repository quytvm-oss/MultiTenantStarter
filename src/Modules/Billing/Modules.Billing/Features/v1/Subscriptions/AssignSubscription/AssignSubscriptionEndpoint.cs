using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Billing.Contracts.Authorization;
using Modules.Billing.Contracts.v1.Subscriptions;

using Shared.Identity.Authorization;

using Web.Idempotency;

namespace Modules.Billing.Features.v1.Subscriptions.AssignSubscription;

public static class AssignSubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapAssignSubscriptionEndpoint(this IEndpointRouteBuilder builder)
    {
        return builder.MapPost("/subscriptions/assign", (AssignSubscriptionCommand command, IMediator mediator, CancellationToken ct)
            => mediator.Send(command, ct))
            .WithName("AssignSubscription")
            .WithSummary("Assign a tenant to a plan, starting now. If the tenant has an existing active subscription it will be cancelled at this moment and replaced.")
            .RequirePermission(BillingPermissions.Manage)
            .WithIdempotency();
    }
}
