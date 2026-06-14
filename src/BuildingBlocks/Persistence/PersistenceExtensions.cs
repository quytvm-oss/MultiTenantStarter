using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Persistence.Inteceptors;

using Shared.Persistence;

namespace Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(nameof(DatabaseOptions)))
            .ValidateDataAnnotations().Validate(o => !string.IsNullOrWhiteSpace(o.Provider), 
                "DatabaseOptions.Provider is required.")
            .ValidateOnStart();

        services.AddHostedService<DatabaseOptionsStartupLogger>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ISaveChangesInterceptor, AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DomainEventsInterceptor>();
        return services;
    }

    public static IServiceCollection AddDbContext<TContext>(this ServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDbContext<TContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.ConfigureCustomDatabase(dbConfig.Provider, dbConfig.ConnectionString, dbConfig.MigrationAssembly, env.IsDevelopment());
            options.AddInterceptors(sp.GetRequiredService<ISaveChangesInterceptor>());
        });
        return services;
    }
}