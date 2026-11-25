using Mediator;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.v1.Tokens.Logout;
using Modules.Identity.Helpers;

namespace Modules.Identity.Features.v1.Tokens.Logout;

public static class LogoutEndpoint
{
    public static RouteHandlerBuilder MapLogoutEndpoint(this IEndpointRouteBuilder endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint.MapPost("/logout",
                [AllowAnonymous] async Task<Results<Ok, UnauthorizedHttpResult, ProblemHttpResult>>
                (
                    [FromHeader(Name = "tenant")] string tenant,
                    [FromServices] IMediator mediator,
                    HttpContext ctx,
                    CancellationToken ct) =>
                {
                    if (!ctx.Request.Cookies.TryGetValue("access_token", out var accessToken) ||
                        !ctx.Request.Cookies.TryGetValue("refresh_token", out var refreshToken) ||
                        string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    {
                        return TypedResults.Unauthorized();
                    }
                    var response = await mediator.Send(new LogoutCommand(refreshToken), ct);
                    ctx.Response.ClearAuthCookies(); 
                    return TypedResults.Ok();
                })
            .WithName("LogoutJwtTokens")
            .WithSummary("Revoke JWT access and refresh tokens")
            .WithDescription("Use a valid (possibly expired) access token together with a valid refresh token to obtain a new access token and a rotated refresh token.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}