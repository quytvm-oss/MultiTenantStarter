using System.Globalization;
using System.Reflection;
using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.OpenTelemetry;

namespace Web.Observability.Logging.Serilog;

public static class Extensions
{
    public static IHostApplicationBuilder AddAppLogging(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<HttpRequestContextEnricher>();

        var otlp = ResolveOtlpLogExport(builder);

        builder.Services.AddSerilog((context, logger) =>
        {
            var httpEnricher = context.GetRequiredService<HttpRequestContextEnricher>();

            // Single source of truth for levels/overrides/sinks — appsettings wins.
            // Programmatic overrides intentionally avoided here; configure via
            // Serilog:MinimumLevel:Override in appsettings instead.
            logger.ReadFrom.Configuration(builder.Configuration);
            logger
                .Enrich.With(httpEnricher)
                // Suppress double-logging: the global exception handler already captures
                // unhandled exceptions; ExceptionHandlerMiddleware re-logs the same event.
                // .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                // .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                // .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                // .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                // .MinimumLevel.Override("Finbuckle.MultiTenant", LogEventLevel.Warning)
                .Filter.ByExcluding(
                    Matching.FromSource(
                        "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"));

            if (otlp is not null)
            {
                logger.WriteTo.OpenTelemetry(sink =>
                {
                    sink.Endpoint = otlp.Endpoint;
                    sink.Protocol = otlp.Protocol;
                    sink.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                                           ?? builder.Environment.ApplicationName
                    };
                    if (otlp.Headers.Count > 0)
                        sink.Headers = otlp.Headers;
                });
            }
        });

        return builder;
    }

    public static IApplicationBuilder UseAppLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RequestQuery", httpContext.Request.QueryString.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                diagnosticContext.Set("Tenant", httpContext.User.FindFirstValue("tenant"));
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            };

            options.GetLevel = (httpContext, _, ex) =>
            {
                if (httpContext.Request.Path.StartsWithSegments("/health"))
                    return LogEventLevel.Verbose;

                if (ex != null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                if (httpContext.Response.StatusCode is >= 300 and < 400)
                    return LogEventLevel.Debug;

                return LogEventLevel.Information;
            };
        });

        return app;
    }

    // -------------------------------------------------------------------------
    // OTLP resolution — Aspire injected env vars win over appsettings config
    // -------------------------------------------------------------------------

    private sealed record OtlpLogExport(string Endpoint, OtlpProtocol Protocol, IDictionary<string, string> Headers);

    private static OtlpLogExport? ResolveOtlpLogExport(IHostApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>();
        // Honor the global OpenTelemetry switch, matching the traces/metrics gate.
        if (options is null || !options.Enabled)
        {
            return null;
        }

        var envEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        string? endpoint;
        string? protocolRaw;
        if (!string.IsNullOrWhiteSpace(envEndpoint))
        {
            // An injected endpoint (Aspire / collector) takes precedence and exports even if config has Otlp disabled.
            endpoint = envEndpoint;
            protocolRaw = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        }
        else if (options.Exporter.Otlp.Enabled)
        {
            endpoint = options.Exporter.Otlp.Endpoint;
            protocolRaw = options.Exporter.Otlp.Protocol;
        }
        else
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        var protocol = protocolRaw?.Trim().ToLowerInvariant() switch
        {
            "http/protobuf" => OtlpProtocol.HttpProtobuf,
            _ => OtlpProtocol.Grpc
        };

        // gRPC uses the base endpoint as-is; for HTTP the Serilog sink expects the full signal path.
        if (protocol == OtlpProtocol.HttpProtobuf &&
            !endpoint.Contains("/v1/logs", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = $"{endpoint.TrimEnd('/')}/v1/logs";
        }

        var headers = ParseOtlpHeaders(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS"));
        return new OtlpLogExport(endpoint, protocol, headers);
    }

    private static Dictionary<string, string> ParseOtlpHeaders(string? raw)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return headers;
        }

        foreach (var pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = pair.IndexOf('=', StringComparison.Ordinal);
            if (idx <= 0)
            {
                continue;
            }

            var key = pair[..idx].Trim();
            if (key.Length > 0)
            {
                headers[key] = pair[(idx + 1)..].Trim();
            }
        }

        return headers;
    }
}