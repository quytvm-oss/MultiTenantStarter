using Jobs.Services;

using Microsoft.EntityFrameworkCore;

using Modules.Auditing;
using Modules.Identity;
using Modules.Identity.Data;
using Modules.Multitenancy;
using Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using Modules.Multitenancy.Data;

using MultiTenantStarter.DbMigrator;

using Web;
using Web.Modules;

// var builder = Host.CreateApplicationBuilder(args);
// builder.Services.AddHostedService<Worker>();
//
// var host = builder.Build();
// host.Run();

var builder = Host.CreateApplicationBuilder(args);

builder.ConfigureContainer(new DefaultServiceProviderFactory(
    new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false }));

builder.Configuration.Sources.Clear();

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("Configurations/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"Configurations/appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies =
    [
        typeof(GetTenantStatusQuery),
    ];
});

// chỉ add platform phần cần thiết
builder.AddPlatform(o =>
{
    o.EnableOpenTelemetry = false;
    o.EnableCors = false;
    o.EnableOpenApi = false;
    o.EnableJobs = false;
    o.EnableMailing = false;
    o.EnableSse = false;
    o.EnableRealtime = false;
    o.EnableCaching = false;
});

// chỉ add module hiện có
builder.AddModules([
    typeof(MultitenancyModule).Assembly,
    typeof(IdentityModule).Assembly,
    typeof(AuditingModule).Assembly
]);

// TenantProvisioningService needs IJobService, but Hangfire's is gated behind EnableJobs (off here).
// Provide a throwing no-op so the DI graph resolves; the migration code paths don't enqueue jobs.
builder.Services.AddSingleton<IJobService, NoOpJobService>();

using var host = builder.Build();

await host.StartAsync();

try
{
    using var scope = host.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

    Console.WriteLine("[tenant-catalog] applying migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("[tenant-catalog] done");
    
    // Identity
    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

    Console.WriteLine("[identity] applying migrations...");
    await identityDb.Database.MigrateAsync();
    Console.WriteLine("[identity] done");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
finally
{
    await host.StopAsync();
}