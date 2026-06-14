using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Shared.Persistence;

namespace Persistence;

public static class OptionsBuilderExtensions
{
    public static DbContextOptionsBuilder ConfigureCustomDatabase(
        this DbContextOptionsBuilder builder,
        string dbProvider, string connectionString,
        string migrationAssembly,bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbProvider);

        builder.ConfigureWarnings(warnings => 
            warnings.Log(RelationalEventId.PendingModelChangesWarning));

        switch (dbProvider.ToUpperInvariant())
        {
            case DbProviders.PostgreSQL:
                builder.UseNpgsql(connectionString, e =>
                {
                    e.MigrationsAssembly(migrationAssembly);
                });
                break;
            case DbProviders.MSSQL:
                builder.UseSqlServer(connectionString, options =>
                {
                    options.MigrationsAssembly(migrationAssembly);
                    options.EnableRetryOnFailure();
                });
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider: '{dbProvider}'.");
        }

        if (isDevelopment)
        {
            builder.EnableSensitiveDataLogging();
            builder.EnableDetailedErrors();
        }
        
        return builder;
    }
}