using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Web.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
{
    private readonly SecurityHeadersOptions _options = options.Value;

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.Enabled)
        {
            return next(context);
        }

        var path = context.Request.Path;

        // Allow listed paths (e.g., OpenAPI / Scalar UI) to manage their own scripts/styles.
        /*if (_options.ExcludedPaths?.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)) == true)
        {
            return next(context);
        }*/

        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-XSS-Protection"] = "0";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
        
        var skipCsp = _options.ExcludedPaths?
            .Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)) == true;
        if (!skipCsp && !headers.ContainsKey("Content-Security-Policy"))
        {
            var script = string.Join(' ', _options.ScriptSources ?? []);
            var style = string.Join(' ', _options.StyleSources ?? []);
            var connect = string.Join(' ', _options.ConnectSources ?? []);

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "img-src 'self' data: https:; " +
                $"script-src 'self' {script}; " +                     
                $"style-src 'self' {(_options.AllowInlineStyles ? "'unsafe-inline' " : "")}{style}; " +
                $"connect-src 'self' {connect}; " +
                "object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
        }

        return next(context);
    }
}