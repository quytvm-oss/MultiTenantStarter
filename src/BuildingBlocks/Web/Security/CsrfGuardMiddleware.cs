using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Web.Cors;

namespace Web.Security;

public sealed class CsrfGuardMiddleware(RequestDelegate next, IOptions<CorsSettings> cors)
{
    private readonly HashSet<string> _origins =
        new(cors.Value.AllowedOrigins, StringComparer.OrdinalIgnoreCase);
    private readonly bool _allowAll = cors.Value.AllowAll;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var m = ctx.Request.Method;
        var isUnsafe = !(HttpMethods.IsGet(m) || HttpMethods.IsHead(m) || HttpMethods.IsOptions(m));
        var hasAuthCookie = ctx.Request.Cookies.ContainsKey("access_token")
                            || ctx.Request.Cookies.ContainsKey("refresh_token");

        if (isUnsafe && hasAuthCookie)
        {
            var origin = ctx.Request.Headers.Origin.ToString();
            var originOk = string.IsNullOrEmpty(origin) || _allowAll || _origins.Contains(origin);
            var headerOk = ctx.Request.Headers.ContainsKey("X-Requested-With");

            if (!originOk || !headerOk)
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }
        
        await next(ctx);
    }
}