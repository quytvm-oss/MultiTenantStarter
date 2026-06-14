using Core.Exceptions;

using Hangfire;
using Hangfire.PostgreSql;

using Jobs.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shared.Persistence;

namespace Jobs;

public static class Extensions
{
    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddOptions<HangfireOptions>().BindConfiguration(nameof(HangfireOptions))
            .ValidateDataAnnotations().ValidateOnStart();
        
        services.AddHangfireServer(options =>
        {
            options.HeartbeatInterval = TimeSpan.FromSeconds(30);
            options.Queues = ["default", "email"];
            options.WorkerCount = 5;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(30);
        });
        
        services.AddHangfire((provider, config) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var dbOptions = configuration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>()
                            ?? throw new CustomException("Database options not found");

            switch (dbOptions.Provider.ToUpperInvariant())
            {
                case DbProviders.PostgreSQL:
                    config.UsePostgreSqlStorage(o =>
                    {
                        o.UseNpgsqlConnection(dbOptions.ConnectionString);
                    });
                    break;

                case DbProviders.MSSQL:
                    config.UseSqlServerStorage(dbOptions.ConnectionString);
                    break;

                default:
                    throw new CustomException($"Hangfire storage provider {dbOptions.Provider} is not supported");
            }

            config.UseActivator(new CustomJobActivator(provider.GetRequiredService<IServiceScopeFactory>()));
            config.UseFilter(new CustomJobFilter(provider));
            config.UseFilter(new LogJobFilter());
            config.UseFilter(new HangfireTelemetryFilter());
        });

        // Deferred stale lock cleanup — runs after app starts accepting requests
        services.AddHostedService<HangfireStaleLockCleanupService>();
        services.AddSingleton<HangfireCustomBasicAuthenticationFilter>();
        services.AddTransient<IJobService, HangfireJobService>();
        
        return services;
    }
    
    public static IApplicationBuilder UseJobDashboard(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var services = app.ApplicationServices;
        var filter = services.GetRequiredService<HangfireCustomBasicAuthenticationFilter>();

        var hangfireOptions = services.GetRequiredService<IOptions<HangfireOptions>>().Value;

        var dashboardOptions = new DashboardOptions
        {
            AppPath = "/",
            Authorization = [filter]
        };

        return app.UseHangfireDashboard(hangfireOptions.Route, dashboardOptions);
    }
}