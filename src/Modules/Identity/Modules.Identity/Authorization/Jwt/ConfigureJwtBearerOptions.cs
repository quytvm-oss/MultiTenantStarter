using System.Security.Claims;
using System.Text;

using Core.Exceptions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
                if (context.Response.HasStarted)
                {
                    return Task.CompletedTask;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Type = "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Authentication is required to access this resource.",
                    Instance = context.HttpContext.Request.Path,
                };

                if (isDev && context.HttpContext.Items[FailureKey] is string reason)
                {
                    problem.Extensions["reason"] = reason;
                }

                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                var result = System.Text.Json.JsonSerializer.Serialize(problem);
                return context.Response.WriteAsync(result);
            },
            OnForbidden = _ => throw new ForbiddenException(),
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    return Task.CompletedTask;
                }

                var path = context.HttpContext.Request.Path;
                // Browser EventSource/SignalR can't send an Authorization header, so they use
                // ?access_token=. The narrow path allow-list keeps query-string tokens from leaking elsewhere.
                if (path.StartsWithSegments("/notifications", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments("/api/v1/realtime/hub", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = accessToken;
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