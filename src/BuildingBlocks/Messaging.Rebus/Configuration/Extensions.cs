
using Messaging.Rebus.Routing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Npgsql;

using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.ServiceProvider;

namespace Messaging.Rebus.Configuration;

public static class Extensions
{
    public static IServiceCollection AddHeroMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string moduleKey,
        bool isPrimary = false)
    {
        var options = configuration.GetSection(nameof(RebusOptions)).Get<RebusOptions>();

        var dbSettings = options?.Database;

        services.TryAddSingleton<IRebusHandlerRegistry, RebusHandlerRegistry>();

        services.TryAddSingleton(_ => NpgsqlDataSource.Create(dbSettings!.ConnectionString));

        services.AddRebus(
            isDefaultBus: isPrimary,
            key: moduleKey,
            configure: (config, provider) => config
                .Transport(t => t.UseRabbitMq(
                    connectionString: options!.RabbitMq.ConnectionString,
                    inputQueueName: moduleKey))
                //.Outbox(o => o.StoreInPostgreSql(
                //    connectionString: dbSettings?.ConnectionString,
                //    tableName: options!.Storage.OutboxTableName))
                .Timeouts(t => t.StoreInPostgres(
                    connectionString: dbSettings?.ConnectionString,
                    tableName: options!.Storage.TimeoutsTableName))
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

        return services;
    }

    public static IServiceCollection AddModuleBus<TModule>(
        this IServiceCollection services,
        string moduleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        services.AddScoped<IModuleBus<TModule>>(sp =>
        {
            var registry = sp.GetRequiredService<IBusRegistry>();
            var bus = registry.GetBus(moduleKey);
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();

            var outboxBus = new OutboxBus(bus, dataSource);

            Console.WriteLine(
                $"Registering {typeof(TModule).Name}: " +
                $"{outboxBus.GetType().FullName}");

            return new ModuleBus<TModule>(outboxBus);
        });

        return services;
    }

    private static void UseQueueHandlers(
        this OptionsConfigurer options,
        IServiceProvider serviceProvider,
        string queueName)
    {
        options.Decorate<IPipeline>(context =>
        {
            var pipeline = context.Get<IPipeline>();

            var registry = serviceProvider.GetRequiredService<IRebusHandlerRegistry>();

            var withoutDefault = new PipelineStepRemover(pipeline)
                        .RemoveIncomingStep(step => step is ActivateHandlersStep);

            var logger = serviceProvider.GetRequiredService<ILogger<QueueActivateHandlersStep>>();

            var queueStep = new QueueActivateHandlersStep(serviceProvider, registry, queueName, logger);

            return new PipelineStepInjector(withoutDefault)
                    .OnReceive(queueStep, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
        });
    }

    public static IServiceCollection AddQueueHandlerRegistry(this IServiceCollection services)
    {
        services.AddSingleton<IRebusHandlerRegistry, RebusHandlerRegistry>();

        return services;
    }

    public static IServiceCollection AddQueueHandler<THandler>(
        this IServiceCollection services,
        string queueName)
        where THandler : class, IHandleMessages
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

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

        // Validate every message type before changing the service collection.
        foreach (var messageType in messageTypes)
        {
            var existing = services
                .Where(x => x.ServiceType == typeof(RebusHandlerDescriptor))
                .Select(x => x.ImplementationInstance)
                .OfType<RebusHandlerDescriptor>()
                .FirstOrDefault(x =>
                    string.Equals(x.QueueName, queueName, StringComparison.OrdinalIgnoreCase)
                    && x.MessageType == messageType);

            if (existing is not null)
            {
                throw new InvalidOperationException(
                    $"Queue '{queueName}' already has handler '{existing.HandlerType.FullName}' " +
                    $"registered for message type '{messageType.FullName}'. " +
                    $"Cannot register handler '{handlerType.FullName}'. " +
                    "Only one handler is allowed per queue and message type.");
            }
        }

        //Quan trọng:
        // Chỉ register concrete handler.
        services.AddTransient<THandler>();

        foreach (var messageType in messageTypes)
        {
            services.AddSingleton(new RebusHandlerDescriptor(
                QueueName: queueName,
                MessageType: messageType,
                HandlerType: handlerType));
        }

        return services;
    }


    public static IServiceCollection AddMessageRoute<TMessage>(
        this IServiceCollection services,
        string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name must not be empty.", nameof(queueName));
        }

        services.AddSingleton(new MessageRouteDescriptor(typeof(TMessage), queueName));

        return services;
    }
}
