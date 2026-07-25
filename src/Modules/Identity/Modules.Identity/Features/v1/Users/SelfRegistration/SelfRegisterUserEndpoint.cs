using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.v1.Users.RegisterUser;

using Shared.Multitenancy;

using Web.Idempotency;

namespace Modules.Identity.Features.v1.Users.SelfRegistration;

public static class SelfRegisterUserEndpoint
{
    internal static RouteHandlerBuilder MapSelfRegisterUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/self-register", async (RegisterUserCommand command,
                [FromHeader(Name = MultitenancyConstants.Identifier)] string tenant,
                HttpContext context,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var origin = $"{context.Request.Scheme}://{context.Request.Host.Value}{context.Request.PathBase.Value}";
                command.Origin = origin;
                var result = await mediator.Send(command, cancellationToken);
                return TypedResults.Created($"/api/v1/identity/users/{result.UserId}", result);
            })
            .WithName("SelfRegisterUser")
            .WithSummary("Self register user")
            .WithDescription("Allow a user to self-register. Anonymous; tenant identified via the tenant header.")
            .AllowAnonymous()
            .WithIdempotency()
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }
}