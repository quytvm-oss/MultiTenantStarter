using System.Reflection;

using MessageBus;
using MessageBus.Persistence.Implementation.PostgreSql;
using MessageBus.Transports.Implementation.RabbitMq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Rebus.Config;
using Rebus.Config.Outbox;
using Rebus.Routing.TypeBased;

using Shared.Persistence;

namespace Web.MessageBus;

public static class Extensions
{
    public static IHostApplicationBuilder AddCustomMessageBus(
        this IHostApplicationBuilder builder,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var databaseOptions =
            builder.Configuration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>()
            ?? new DatabaseOptions();

        builder.Services.AddMessageBus(options =>
        {
            options
                .UsePostgreSql(databaseOptions.ConnectionString)
                .UseRabbitMQ(rabbit =>
                {
                    rabbit.HostName = "localhost";
                    rabbit.UserName = "guest";
                    rabbit.Password = "guest";
                    rabbit.ExchangeName = "multitenant";
                })
                .AddConsumerRegistrationsFromAssemblies(assemblies);
        });

        return builder;
    }
    
    public static IServiceCollection AddHeroMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbSettings = configuration
            .GetSection(nameof(DatabaseOptions))
            .Get<DatabaseOptions>();

        var options = configuration
            .GetSection(nameof(RebusOptions))
            .Get<RebusOptions>() ?? new RebusOptions();

        services.AddRebus(config => config
            .Transport(t => t.UseRabbitMq(
                connectionString: options.RabbitMq.ConnectionString,
                inputQueueName: options.QueueName))
            // .Subscriptions(s => s.StoreInPostgres(
            //     connectionString: dbSettings?.ConnectionString,
            //     tableName: options.Storage.SubscriptionsTableName,
            //     isCentralized: true))
            .Outbox(o => o.StoreInPostgreSql(
                connectionString: dbSettings?.ConnectionString,
                tableName:  options.Storage.OutboxTableName))
            .Routing(r => r.TypeBased().MapFallback(options.QueueName))
            .Options(o =>
            {
                o.SetNumberOfWorkers(options.NumberOfWorkers);
                o.SetMaxParallelism(options.MaxParallelism);
            })
            .Logging(l => l.Serilog()));

        //services.AutoRegisterHandlersFromAssemblyOf<TMarker>();

        return services;
    }
    
    public static IServiceCollection AddHeroMessagingModules(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            services.AutoRegisterHandlersFromAssembly(assembly);
        }

        return services;
    }
}