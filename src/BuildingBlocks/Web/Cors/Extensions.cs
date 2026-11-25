using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Web.Cors;

public static class Extensions
{
    private const string PolicyName = "CorsPolicy";
    
    private static readonly string[] RequiredHeaders = ["Content-Type", "tenant", "X-Requested-With"];

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        
        services.AddOptions<CorsSettings>()
            .Bind(configuration.GetSection(nameof(CorsSettings)))
            .Validate(s => s.AllowAll || s.AllowedOrigins.Length > 0,
                "CorsSettings: AllowedOrigins are required when AllowAll is false.")
            .Validate(s => s.AllowAll || s.AllowedMethods.Length > 0,
                "CorsSettings: AllowedMethods are required when AllowAll is false.")
            .Validate(s => s.AllowedOrigins.All(IsValidOrigin),
                "CorsSettings: each origin must be scheme://host[:port], no trailing slash, no path, no wildcard.")
            .ValidateOnStart();

        services.AddCors();
        services.AddSingleton<IConfigureOptions<CorsOptions>>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CorsSettings>>().Value;
            var env = sp.GetRequiredService<IHostEnvironment>();
            
            if (settings.AllowAll && !env.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "CorsSettings.AllowAll is not permitted outside Development when cookie authentication is used.");
            }

            return new ConfigureOptions<CorsOptions>(opt =>
            {
                opt.AddPolicy(PolicyName, builder =>
                {
                    if (settings.AllowAll)
                    {
                        // Echo the request origin (not `*`): the CORS spec forbids `*` with credentialed requests,
                        // and SignalR's negotiate always runs credentialed — so AllowCredentials needs a specific origin.
                        builder
                            .SetIsOriginAllowed(_ => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        var headers = settings.AllowedHeaders
                            .Concat(RequiredHeaders)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();
                        
                        builder
                            .WithOrigins(settings.AllowedOrigins)
                            .WithHeaders(headers)
                            .WithMethods(settings.AllowedMethods)
                            .AllowCredentials()
                            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                    }
                });
            });
        });
        
        return services;
    }
    
    public static void UseCorsPolicy(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseCors(PolicyName);
    }
    
    private static bool IsValidOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && uri.AbsolutePath == "/"
        && !origin.EndsWith('/')
        && !origin.Contains('*');
}