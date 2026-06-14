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

        var otlp = ResolveOtlpExport(builder);
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
        
        builder.Services.AddSerilog((services, logger) =>
        {
            var httpEnricher = services.GetRequiredService<HttpRequestContextEnricher>();
            var options = builder.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>();

            // Log levels — từ config hoặc defaults
            if (options?.LogLevel is { Count: > 0 })
            {
                foreach (var (key, value) in options.LogLevel)
                {
                    var level = ToSerilogLevel(value);
                    if (key == "Default")
                        logger.MinimumLevel.Is(level);
                    else
                        logger.MinimumLevel.Override(key, level);
                }
            }
            else
            {
                logger
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                    .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                    .MinimumLevel.Override("Finbuckle.MultiTenant", LogEventLevel.Warning);
            }

            logger
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithCorrelationId()
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("Assembly", assemblyName)
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName)
                .Enrich.With(httpEnricher)
                .Filter.ByExcluding(
                    Matching.FromSource("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"))
                .WriteTo.Console();

            // File sink
            if (options?.File?.Enabled == true)
            {
                logger.WriteTo.File(
                    path: options.File.Path,
                    rollingInterval: options.File.RollingInterval,
                    fileSizeLimitBytes: options.File.FileSizeLimitBytes,
                    rollOnFileSizeLimit: options.File.RollOnFileSizeLimit,
                    retainedFileCountLimit: options.File.RetainedFileCountLimit,
                    restrictedToMinimumLevel: options.File.MinimumLevel,
                    formatProvider: CultureInfo.InvariantCulture,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] " +
                    "[TraceId: {TraceId}] [MachineName: {MachineName}] [ProcessId: {ProcessId}] " +
                    "[Tenant: {Tenant}] [UserId: {UserId}] " +
                    "{Message:lj}{NewLine}{Exception}");
            }

            // Seq sink
            if (options?.Seq?.Enabled == true)
                logger.WriteTo.Seq(options.Seq.ServerUrl);

            // OTLP sink — Aspire injected endpoint takes precedence over config
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

    private sealed record OtlpExport(string Endpoint, OtlpProtocol Protocol, IDictionary<string, string> Headers);

    private static OtlpExport? ResolveOtlpExport(IHostApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue("OpenTelemetryOptions:Enabled", true))
            return null;

        var envEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        string? endpoint;
        string? protocolRaw;

        if (!string.IsNullOrWhiteSpace(envEndpoint))
        {
            endpoint = envEndpoint;
            protocolRaw = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        }
        else if (builder.Configuration.GetValue("OpenTelemetryOptions:Exporter:Otlp:Enabled", false))
        {
            endpoint = builder.Configuration["OpenTelemetryOptions:Exporter:Otlp:Endpoint"];
            protocolRaw = builder.Configuration["OpenTelemetryOptions:Exporter:Otlp:Protocol"];
        }
        else
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
            return null;

        var protocol = protocolRaw?.Trim().ToLowerInvariant() switch
        {
            "http/protobuf" => OtlpProtocol.HttpProtobuf,
            _ => OtlpProtocol.Grpc
        };

        if (protocol == OtlpProtocol.HttpProtobuf &&
            !endpoint.Contains("/v1/logs", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = $"{endpoint.TrimEnd('/')}/v1/logs";
        }

        var headers = ParseOtlpHeaders(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS"));
        return new OtlpExport(endpoint, protocol, headers);
    }

    private static Dictionary<string, string> ParseOtlpHeaders(string? raw)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return headers;

        foreach (var pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = pair.IndexOf('=', StringComparison.Ordinal);
            if (idx <= 0) continue;

            var key = pair[..idx].Trim();
            if (key.Length > 0)
                headers[key] = pair[(idx + 1)..].Trim();
        }

        return headers;
    }

    private static LogEventLevel ToSerilogLevel(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Fatal
        };
}