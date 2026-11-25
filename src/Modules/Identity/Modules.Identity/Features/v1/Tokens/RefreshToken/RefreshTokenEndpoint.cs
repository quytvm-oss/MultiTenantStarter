using Mediator;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using Modules.Identity.Helpers;

namespace Modules.Identity.Features.v1.Tokens.RefreshToken;

public static class RefreshTokenEndpoint
{
    public static RouteHandlerBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint.MapPost("/token/refresh",
                [AllowAnonymous] async Task<Results<Ok<RefreshTokenCommandResponse>, UnauthorizedHttpResult, ProblemHttpResult>>
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
                    var response = await mediator.Send(new RefreshTokenCommand(accessToken, refreshToken), ct);
                    /*ctx.Response.Cookies.Append("access_token", response.AccessToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = response.AccessTokenExpiresAt,
                        Path = "/"
                    });
                    ctx.Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = response.RefreshTokenExpiresAt,
                        Path = "/api/v1/identity/token/refresh"
                    });*/
                    ctx.Response.SetAuthCookies(response.AccessToken,response.AccessTokenExpiresAt,
                        response.RefreshToken,response.RefreshTokenExpiresAt);
                    return TypedResults.Ok(response);
                })
            .WithName("RefreshJwtTokens")
            .WithSummary("Refresh JWT access and refresh tokens")
            .WithDescription("Use a valid (possibly expired) access token together with a valid refresh token to obtain a new access token and a rotated refresh token.")
            .Produces<RefreshTokenCommandResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}