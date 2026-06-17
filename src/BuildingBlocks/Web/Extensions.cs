using Caching.V2;

using Jobs;

using Mailling;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Persistence;

using Web.Auth;
using Web.Cors;
using Web.Exceptions;
using Web.FeatureFlags;
using Web.Health;
using Web.Idempotency;
using Web.Mediator.Behaviors;
using Web.Modules;
using Web.Observability.Logging.Serilog;
using Web.Observability.OpenTelemetry;
using Web.OpenApi;
using Web.Origin;
using Web.RateLimiting;
using Web.Realtime;
using Web.Security;
using Web.Sse;
using Web.Versioning;

namespace Web;

public static class Extensions
{
    public static IHostApplicationBuilder AddPlatform(this IHostApplicationBuilder builder,
        Action<PlatformOptions>? configure = null)
    {
         ArgumentNullException.ThrowIfNull(builder);

        var options = new PlatformOptions();
        configure?.Invoke(options);

        //PermissionConstants.Register(SystemPermissions.All);

        builder.Services.AddScoped<CurrentUserMiddleware>();

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        builder.AddAppLogging();
        if (options.EnableOpenTelemetry)
        {
            builder.AddAppOpenTelemetry();
        }

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddRateLimiting(builder.Configuration);

        var corsEnabled = options.EnableCors && IsCorsEnabled(builder.Configuration);
        var openApiEnabled = options.EnableOpenApi && IsOpenApiEnabled(builder.Configuration);

        if (corsEnabled)
        {
            builder.Services.AddCorsPolicy(builder.Configuration);
        }

        builder.Services.AddVersioning();

        if (openApiEnabled)
        {
            builder.Services.AddAppOpenApi(builder.Configuration);
        }

        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

        if (options.EnableJobs)
        {
            builder.Services.AddJobs();
            builder.Services.AddHealthChecks().AddCheck<HangfireHealthCheck>("hangfire");
        }

        if (options.EnableMailing)
        {
            builder.Services.AddAppMailing(builder.Configuration);
        }

        if (options.EnableCaching)
        {
            builder.Services.AddCachingV2(builder.Configuration);
            var cacheConfig = builder.Configuration.GetSection(nameof(CachingOptions)).Get<CachingOptions>();
            if (cacheConfig is not null && !string.IsNullOrEmpty(cacheConfig.Redis))
            {
                builder.Services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
            }
        }

        if (options.EnableFeatureFlags)
        {
            builder.Services.AddFeatureFlags(builder.Configuration);
        }

        if (options.EnableIdempotency)
        {
            builder.Services.AddHeroIdempotency(builder.Configuration);
        }

        if (options.EnableSse)
        {
            builder.Services.AddSse();
        }

        if (options.EnableRealtime)
        {
            builder.Services.AddRealtime(builder.Configuration);
        }

        // if (options.EnableQuotas)
        // {
        //     builder.Services.AddHeroQuotas(builder.Configuration);
        // }

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddProblemDetails();
        builder.Services.AddOptions<OriginOptions>().BindConfiguration(nameof(OriginOptions));
        builder.Services.AddOptions<SecurityHeadersOptions>().BindConfiguration(nameof(SecurityHeadersOptions));

        return builder;
    }
    
    
     public static WebApplication UsePlatform(this WebApplication app, Action<PipelineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = new PipelineOptions();
        configure?.Invoke(options);

        var corsEnabled = options.UseCors && IsCorsEnabled(app.Configuration);
        var openApiEnabled = options.UseOpenApi && IsOpenApiEnabled(app.Configuration);

        app.UseExceptionHandler();
        app.UseResponseCompression();

        // CORS MUST run before UseHttpsRedirection: preflight OPTIONS can't follow an HTTP→HTTPS redirect, so
        // the browser would block the call. Safe before routing because we use one global policy (no [EnableCors]).
        if (corsEnabled)
        {
            app.UseCorsPolicy();
        }

        app.UseHttpsRedirection();

        app.UseSecurityHeaders();

        // Serve static files as early as possible to short-circuit pipeline
        if (options.ServeStaticFiles)
        {
            var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(assetsPath))
            {
                Directory.CreateDirectory(assetsPath);
            }

            app.UseStaticFiles();
        }

        app.UseJobDashboard();
        app.UseRouting();

        if (openApiEnabled)
        {
            app.UseAppOpenApi();
        }

        //app.UseAuthentication();

        // Let each module register its own middleware (e.g. Auditing registers AuditHttpMiddleware)
        app.UseModuleMiddlewares();

        app.UseRateLimiting();

        // if (options.UseQuotas)
        // {
        //     app.UseHeroQuotas();
        // }

        // app.UseAuthorization();

        if (options.MapModules)
        {
            app.UseModuleEndpoints();
        }

        // Always expose health endpoints
        app.MapHeroHealthEndpoints();

        if (options.MapSseEndpoints)
        {
            app.MapSseEndpoints();
        }

        if (options.MapRealtime)
        {
            app.MapHeroRealtime();
        }
        app.UseMiddleware<CurrentUserMiddleware>();
        return app;
    }
    
    
    private static bool IsCorsEnabled(IConfiguration configuration)
    {
        var allowAll = configuration.GetValue("CorsOptions:AllowAll", false);
        var origins = configuration.GetSection("CorsOptions:AllowedOrigins").Get<string[]>() ?? [];
        return allowAll || origins.Length > 0;
    }

    private static bool IsOpenApiEnabled(IConfiguration configuration)
    {
        return configuration.GetValue("OpenApiOptions:Enabled", true);
    }
}