using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Modules.Multitenancy.Data;

public class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        // Design-time factory: read configuration (appsettings + env vars) to decide provider and connection.
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Configurations/appsettings.json", optional: false)
            .AddJsonFile($"Configurations/appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = configuration["DatabaseOptions:Provider"] ?? "POSTGRESQL";
        var connectionString = configuration["DatabaseOptions:ConnectionString"]
                               ?? "Host=localhost;Database=multitenant;Username=postgres;Password=31072001";
        var migrationsAssembly = configuration["DatabaseOptions:MigrationsAssembly"]
                                 ?? "FSH.Starter.Migrations.PostgreSQL";
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        switch (provider.ToUpperInvariant())
        {
            case "POSTGRESQL":
                optionsBuilder.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(migrationsAssembly));
                break;
            default:
                throw new NotSupportedException($"Database provider '{provider}' is not supported for TenantDbContext migrations.");
        }

        return new TenantDbContext(optionsBuilder.Options);
    }
}