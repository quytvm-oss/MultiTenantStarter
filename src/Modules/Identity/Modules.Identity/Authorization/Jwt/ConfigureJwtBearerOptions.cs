using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Caching.V2;

using Core.Exceptions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Modules.Identity.Contracts.Services;

using Shared.Identity;

namespace Modules.Identity.Authorization.Jwt;

public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _options;
    private readonly IHostEnvironment _environment;

    public ConfigureJwtBearerOptions(IOptions<JwtOptions> options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        _options = options.Value;
        _environment = environment;
    }

    public void Configure(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        Configure(string.Empty, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;
        
        byte[] key = Encoding.ASCII.GetBytes(_options.SigningKey);

        options.RequireHttpsMetadata = true;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidIssuer = _options.Issuer,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidAudience = _options.Audience,
            ValidateAudience = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        // Capture the validation failure reason so OnChallenge can include it (in Development).
        // Without this we get a body of `{"error":"Unauthorized"}` with no clue why JwtBearer rejected.
        const string FailureKey = "JwtAuthFailure";
        bool isDev = _environment.IsDevelopment();
        options.Events = new JwtBearerEvents()
        {
            OnAuthenticationFailed = context =>
            {
                // Stash the exception type+message on HttpContext so OnChallenge can surface it.
                context.HttpContext.Items[FailureKey] =
                    $"{context.Exception.GetType().Name}: {context.Exception.Message}";

                // Server-side log so we can also see the rejection reason in the API console.
                var failedLogger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Identity.JwtAuth");
                failedLogger.LogWarning(context.Exception,
                    "JwtBearer authentication FAILED for {Method} {Path}: {Reason}",
                    SanitizeForLog(context.HttpContext.Request.Method),
                    SanitizeForLog(context.HttpContext.Request.Path.ToString()),
                    context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";

                    // Was an Authorization header even sent? Helps distinguish "JWT rejected"
                    // from "no token at all" — both produce 401 but for very different reasons.
                    bool hadAuthHeader = !string.IsNullOrEmpty(context.HttpContext.Request.Headers.Authorization)
                                         || context.HttpContext.Request.Cookies.ContainsKey("access_token");

                    // RFC 9457 ProblemDetails — matches the contract the rest of the API uses
                    // for error responses (via the global exception handler).
                    var problem = new ProblemDetails
                    {
                        Type = "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = "Authentication is required to access this resource.",
                        Instance = context.HttpContext.Request.Path,
                    };

                    // In Development surface the actual JwtBearer rejection reason; in Production keep the
                    // body opaque to avoid leaking validation internals.
                    if (isDev)
                    {
                        if (context.HttpContext.Items[FailureKey] is string reason)
                        {
                            problem.Extensions["reason"] = reason;
                        }
                        else if (!hadAuthHeader)
                        {
                            problem.Extensions["reason"] = "No Authorization header on the request.";
                        }
                        else
                        {
                            // Header present but JwtBearer didn't fire OnAuthenticationFailed —
                            // typically means the bearer scheme didn't match the AuthorizationPolicy.
                            problem.Extensions["reason"] = "Bearer token present but JwtBearer did not validate it (scheme mismatch?).";
                        }

                        var challengeLogger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("FSH.Identity.JwtAuth");
                        challengeLogger.LogWarning(
                            "JwtBearer challenge for {Method} {Path}: hadAuthHeader={HadHeader} reason={Reason}",
                            SanitizeForLog(context.HttpContext.Request.Method),
                            SanitizeForLog(context.HttpContext.Request.Path.ToString()),
                            hadAuthHeader,
                            problem.Extensions["reason"]);
                    }

                    var traceId = context.HttpContext.TraceIdentifier;
                    if (!string.IsNullOrEmpty(traceId))
                    {
                        problem.Extensions["traceId"] = traceId;
                    }

                    var result = System.Text.Json.JsonSerializer.Serialize(problem);
                    return context.Response.WriteAsync(result);
                }
                return Task.CompletedTask;
            },
            // Server-side teeth behind /impersonation/revoke: for an impersonation token, reject if its
            // grant is revoked/ended — otherwise revocation wouldn't stop tokens already in flight.
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                
                // 1. Session revoke:
                var sid = principal?.FindFirstValue("sid");
                if (!string.IsNullOrEmpty(sid)) 
                {
                    var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                    var revokedSession = await cache.GetStringAsync(
                        CacheKeys.SessionRevoked(sid), context.HttpContext.RequestAborted);

                    if (revokedSession is not null)
                    {
                        context.HttpContext.Items[FailureKey] = "Session revoked";
                        context.Fail("session revoked");
                        return;
                    }
                }
                
                var actSub = principal?.FindFirstValue(ClaimConstants.ActorSubject);
                if (string.IsNullOrEmpty(actSub))
                {
                    // Not an impersonation token — zero cost for normal sessions.
                    return;
                }

                var jti = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (string.IsNullOrEmpty(jti))
                {
                    // jti is always minted by Start; reject defensively so a malformed/stripped token
                    // can't bypass the revocation check.
                    context.HttpContext.Items[FailureKey] = "Impersonation token missing jti claim";
                    context.Fail("impersonation token missing jti claim");
                    return;
                }

                // Resolve in a CHILD scope: this hook runs before Finbuckle resolves the tenant, so a request-
                // scoped IdentityDbContext would cache a null-tenant context and NRE later tenant query filters.
                await using var hookScope = context.HttpContext.RequestServices.CreateAsyncScope();
                var grants = hookScope.ServiceProvider
                    .GetRequiredService<IImpersonationGrantService>();
                var revoked = await grants
                    .IsRevokedOrEndedAsync(jti, context.HttpContext.RequestAborted)
                    .ConfigureAwait(false);

                if (revoked)
                {
                    context.HttpContext.Items[FailureKey] = "Impersonation grant revoked or ended";
                    context.Fail("impersonation grant revoked or ended");
                }
            },
            OnForbidden = _ => throw new ForbiddenException(),
            OnMessageReceived = context =>
            {
                
                // 1. Có header Bearer thì để JwtBearer xử lý mặc định
                var authHeader = context.Request.Headers.Authorization.ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }
                
                
                /*var accessToken = context.Request.Query["access_token"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    return Task.CompletedTask;
                }*/

                // 2. Query string: chỉ cho SignalR/EventSource
                var path = context.HttpContext.Request.Path;
                var queryToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(queryToken)
                    && (path.StartsWithSegments("/notifications", StringComparison.OrdinalIgnoreCase)
                        || path.StartsWithSegments("/api/v1/realtime/hub", StringComparison.OrdinalIgnoreCase)))
                {
                    context.Token = queryToken;
                    return Task.CompletedTask;
                }
                
                // 3. Cookie
                if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken)
                    && !string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
        
    }

    // Strip control chars so attacker-controlled request data can't forge log lines
    // (CodeQL cs/log-injection); defence in depth on top of Kestrel's URI validation.
    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            buffer.Append(char.IsControl(c) ? '_' : c);
        }
        return buffer.ToString();
    }
}