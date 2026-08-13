using System.Reflection;
using System.Text.Json.Serialization;

using Modules.Auditing;
using Modules.Auditing.Contracts;
using Modules.Files;
using Modules.Identity;
using Modules.Identity.Contracts;
using Modules.Multitenancy;
using Modules.Multitenancy.Contracts.v1.CreateTenant;
using Modules.Multitenancy.Features.v1.CreateTenant;
using Modules.Notifications;

using Web;
using Web.MessageBus;
using Web.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();

builder.Configuration
    .AddJsonFile("Configurations/appsettings.json", false, true)
    .AddJsonFile(
        $"Configurations/appsettings.{builder.Environment.EnvironmentName}.json",
        true,
        true)
    .AddEnvironmentVariables();

// Serialize enums as string names (reads still accept names or integers). [Flags] enums (AuditTag, BodyCapture)
// opt back to numeric via their own NumericEnumConverter since comma-joined flag strings break bitwise consumers. Frontends mirror this as string unions.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

if (builder.Environment.IsProduction())
{
    static void Require(IConfiguration config, string key)
    {
        if (string.IsNullOrWhiteSpace(config[key]))
        {
            throw new InvalidOperationException($"Missing required configuration '{key}' in Production.");
        }
    }

    var config = builder.Configuration;
    Require(config, "DatabaseOptions:ConnectionString");
    Require(config, "CachingOptions:Redis");
    Require(config, "JwtOptions:SigningKey");
}

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies =
    [
        typeof(IdentityContractsMarker),
        typeof(IdentityModule),
        typeof(AuditingContractsMarker),
        typeof(AuditingModule),
        typeof(CreateTenantCommand),
        typeof(CreateTenantCommandHandler)
    ];
});

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(NotificationsModule).Assembly,
    typeof(FilesModule).Assembly,
};

builder.AddPlatform(o =>
{
    o.EnableCaching = true;
    o.EnableMailing = true;
    o.EnableJobs = true;
    o.EnableSse = true;
    o.EnableRealtime = true;
});

builder.AddModules(moduleAssemblies);
builder.Services.AddHeroMessaging(builder.Configuration);
builder.Services.AddHeroMessagingModules(moduleAssemblies);
//builder.AddCustomMessageBus(moduleAssemblies);


var app = builder.Build();

app.UseMultiTenantDatabases();
app.UsePlatform(p =>
{
    p.MapModules = true;
    p.ServeStaticFiles = true;
    p.UseQuotas = true;
    p.MapSseEndpoints = true;
    p.MapRealtime = true;
});

app.MapGet("/", () => Results.Ok(new { message = "hello world!" }))
    .WithTags("PlayGround")
    .AllowAnonymous();
await app.RunAsync();