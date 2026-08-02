using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Impersonation.EndImpersonation;

namespace Modules.Identity.Features.v1.Impersonation.EndImpersonation;

public static class EndImpersonationEndpoint
{
    internal static RouteHandlerBuilder MapEndImpersonationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/impersonation/end",
            async (IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new EndImpersonationCommand(), ct)))
            .WithName("EndImpersonation")
            .WithSummary("End user impersonation")
            .WithDescription("Returns a fresh access + refresh token for the original actor based on the act_sub/act_tenant claims embedded in the impersonation token. Callable by any authenticated impersonation session.")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest);
    }
}