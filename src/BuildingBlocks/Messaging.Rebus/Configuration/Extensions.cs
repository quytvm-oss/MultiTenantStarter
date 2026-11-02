using Messaging.Rebus.Routing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Npgsql;

using Rebus.Bus;
using Rebus.Config;
using Rebus.Config.Outbox;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;

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
                .Outbox(o => o.StoreInPostgreSql(
                    connectionString: dbSettings?.ConnectionString,
                    tableName: options!.Storage.OutboxTableName))
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
                    //o.LogPipeline();
                })
                .Logging(l => l.Serilog()));


        if (isPrimary)
        {
            services.TryDecorate<IBus, OutboxBus>();
        }

        services.TryAddSingleton<IRebusHandlerRegistry, RebusHandlerRegistry>();

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

            var queueStep = new QueueActivateHandlersStep(serviceProvider, registry, queueName);

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
        string queueName,
        string handlerKey)
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
            services.AddSingleton(new RebusHandlerDescriptor(
                QueueName: queueName,
                MessageType: messageType,
                HandlerType: handlerType,
                HandlerKey: handlerKey));
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
