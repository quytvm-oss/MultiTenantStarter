using System.Reflection;

using MessageBus;
using MessageBus.Persistence.Implementation.PostgreSql;
using MessageBus.Transports.Implementation.RabbitMq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Npgsql;

using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Config.Outbox;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;

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

        // QUAN TRỌNG: OutboxBus cần cái này
        services.AddSingleton<NpgsqlDataSource>(_ =>
            NpgsqlDataSource.Create(dbSettings!.ConnectionString));

        services.AddRebus(config => config
            .Transport(t => t.UseRabbitMq(
                connectionString: options.RabbitMq.ConnectionString,
                inputQueueName: options.QueueName))
            .Outbox(o => o.StoreInPostgreSql(
                connectionString: dbSettings?.ConnectionString,
                tableName: options.Storage.OutboxTableName))
            .Routing(r => r.TypeBased().MapFallback(options.QueueName))
            .Options(o =>
            {
                o.SetNumberOfWorkers(options.NumberOfWorkers);
                o.SetMaxParallelism(options.MaxParallelism);
            })
            .Logging(l => l.Serilog()));

        services.Decorate<IBus, OutboxBus>();

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

    public static IServiceCollection AddHeroMessaging(this IServiceCollection services, IConfiguration configuration,
        string moduleKey, bool isPrimary = false)
    {
        var dbSettings = configuration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>();
        
        var options = configuration.GetSection(nameof(RebusOptions)).Get<RebusOptions>();

        services.TryAddSingleton<IRebusHandlerRegistry, RebusHandlerRegistry>();

        services.TryAddSingleton(_ => NpgsqlDataSource.Create(dbSettings!.ConnectionString));

        services.AddRebus(
            isDefaultBus: isPrimary,
            key: moduleKey,
            configure: (config, provider) => config
                .Transport(t => t.UseRabbitMq(
                    connectionString: options!.RabbitMq.ConnectionString,
                    inputQueueName: moduleKey))
                .Outbox(o => o.StoreInPostgreSql(
                    connectionString: dbSettings?.ConnectionString,
                    tableName: options!.Storage.OutboxTableName))
                .Timeouts(t => t.StoreInPostgres(
                    connectionString: dbSettings?.ConnectionString,
                    tableName: options!.Storage.TimeoutsTableName))
                // .Routing(r => r.TypeBased().MapFallback(moduleKey))
                .Routing(r =>
                {
                    var routes = provider.GetServices<MessageRouteDescriptor>();
                    var typeBased = r.TypeBased();
                    foreach (var route in routes)
                    {
                        typeBased.Map(route.MessageType, route.QueueName);
                    }
                    typeBased.MapFallback(moduleKey);
                })
                .Options(o =>
                {
                    o.SetNumberOfWorkers(options!.NumberOfWorkers);
                    o.SetMaxParallelism(options!.MaxParallelism);
                    o.UseQueueHandlers(provider, moduleKey);
                    o.LogPipeline();
                })
                .Logging(l => l.Serilog()));


        if (isPrimary)
        {
            services.Decorate<IBus, OutboxBus>();
        }
        //services.TryDecorate<IBus, OutboxBus>();

        services.TryAddSingleton<IRebusHandlerRegistry, RebusHandlerRegistry>();

        return services;
    }
    
    public static IServiceCollection AddQueueHandler<THandler>(this IServiceCollection services, string queueName, string handlerKey)
        where THandler : class, IHandleMessages
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerKey);

        var handlerType = typeof(THandler);

        var messageTypes = handlerType.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
            .Select(x => x.GetGenericArguments()[0])
            .Distinct()
            .ToArray();

        if (messageTypes.Length == 0)
        {
            throw new InvalidOperationException($"{handlerType.FullName} does not implement IHandleMessages<TMessage>.");
        }

        // Quan trọng:
        // Chỉ register concrete handler.
        services.AddTransient<THandler>();

        foreach (var messageType in messageTypes)
        {
            services.AddSingleton(new RebusHandlerDescriptor(QueueName: queueName, MessageType: messageType, HandlerType: handlerType, HandlerKey: handlerKey));
        }

        return services;
    }

    public static IServiceCollection AddQueueHandlerRegistry(this IServiceCollection services)
    {
        services.AddSingleton<IRebusHandlerRegistry, RebusHandlerRegistry>();

        return services;
    }
    
    private static void UseQueueHandlers(this OptionsConfigurer options, IServiceProvider serviceProvider, string queueName)
    {
        options.Decorate<IPipeline>(context =>
        {
            var pipeline = context.Get<IPipeline>();

            var registry = serviceProvider.GetRequiredService<IRebusHandlerRegistry>();

            var withoutDefault = new PipelineStepRemover(pipeline).RemoveIncomingStep(step => step is ActivateHandlersStep);

            var queueStep = new QueueActivateHandlersStep(serviceProvider, registry, queueName);

            return new PipelineStepInjector(withoutDefault).OnReceive(queueStep, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
        });
    }
    
    public static IServiceCollection AddMessageRoute<TMessage>(this IServiceCollection services, string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name must not be empty.", nameof(queueName));
        }

        services.AddSingleton(new MessageRouteDescriptor(typeof(TMessage), queueName));

        return services;
    }
}