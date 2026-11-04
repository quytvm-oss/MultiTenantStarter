using Messaging.Rebus.Routing;

using Microsoft.Extensions.Configuration;
using Messaging.Rebus.Inbox;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);
        var options = configuration.GetSection(nameof(RebusOptions)).Get<RebusOptions>() ?? new RebusOptions();
        if (string.IsNullOrWhiteSpace(options.Database.ConnectionString))
            options.Database.ConnectionString = configuration["DatabaseOptions:ConnectionString"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(options.Database.ConnectionString))
            throw new InvalidOperationException("Configure RebusOptions:Database:ConnectionString or DatabaseOptions:ConnectionString.");
        if (string.IsNullOrWhiteSpace(options.RabbitMq.ConnectionString))
            throw new InvalidOperationException("Configure RebusOptions:RabbitMq:ConnectionString.");
        options.Inbox.Validate();
        var dbSettings = options.Database;

        services.TryAddSingleton<IRebusHandlerRegistry, RebusHandlerRegistry>();

        services.TryAddSingleton(_ => NpgsqlDataSource.Create(dbSettings.ConnectionString));
        services.TryAddSingleton(options.Inbox);
        services.TryAddSingleton<IInboxStore, PostgreSqlInboxStore>();
        if (options.Inbox.Enabled)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, InboxCleanupService>());

        services.AddRebus(
            isDefaultBus: isPrimary,
            key: moduleKey,
            configure: (config, provider) => config
                .Transport(t => t.UseRabbitMq(
                    connectionString: options.RabbitMq.ConnectionString,
                    inputQueueName: moduleKey))
                .Outbox(o => o.StoreInPostgreSql(
                    connectionString: dbSettings.ConnectionString,
                    tableName: options.Storage.OutboxTableName))
                .Timeouts(t => t.StoreInPostgres(
                    connectionString: dbSettings.ConnectionString,
                    tableName: options.Storage.TimeoutsTableName))
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
                    o.SetNumberOfWorkers(options.NumberOfWorkers);
                    o.SetMaxParallelism(options.MaxParallelism);
                    o.UseQueueHandlers(provider, moduleKey, options.Inbox);
                })
                .Logging(l => l.Serilog()));

        if (isPrimary)
            services.Decorate<IBus, OutboxBus>();

        return services;
    }

    public static IServiceCollection AddModuleBus<TModule>(
        this IServiceCollection services,
        string moduleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        services.TryAddSingleton<IModuleBus<TModule>>(sp =>
        {
            var registry = sp.GetRequiredService<IBusRegistry>();
            var bus = registry.GetBus(moduleKey);
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();

            return new ModuleBus<TModule>(new OutboxBus(bus, dataSource));
        });

        return services;
    }

    internal static void UseQueueHandlers(
        this OptionsConfigurer options,
        IServiceProvider serviceProvider,
        string queueName,
        InboxOptions inboxOptions,
        Func<IBus>? getBus = null)
    {
        options.Decorate<IPipeline>(context =>
        {
            var pipeline = context.Get<IPipeline>();

            var registry = serviceProvider.GetRequiredService<IRebusHandlerRegistry>();

            var withoutDefault = new PipelineStepRemover(pipeline)
                .RemoveIncomingStep(step => step is ActivateHandlersStep
                    // Rebus.PostgreSql 9.1.1 registers its DB commit before flushing
                    // outgoing messages; replace it with ordered explicit completion.
                    || step.GetType().FullName == "Rebus.PostgreSql.Outbox.OutboxIncomingStep");

            var queueStep = new QueueActivateHandlersStep(serviceProvider, registry, queueName);

            IPipeline result = new PipelineStepInjector(withoutDefault)
                .OnReceive(queueStep, PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
            result = new PipelineStepInjector(result).OnReceive(
                new IncomingOutboxTransactionStep(serviceProvider.GetRequiredService<NpgsqlDataSource>()),
                PipelineRelativePosition.Before, typeof(QueueActivateHandlersStep));
            if (inboxOptions.Enabled)
            {
                result = new PipelineStepInjector(result).OnReceive(
                    new InboxIncomingStep(serviceProvider.GetRequiredService<IInboxStore>(), inboxOptions,
                        queueName, serviceProvider.GetRequiredService<ILogger<InboxIncomingStep>>(),
                        delay => (getBus?.Invoke() ?? serviceProvider.GetRequiredService<IBusRegistry>().GetBus(queueName))
                            .Advanced.TransportMessage.Defer(delay)),
                    PipelineRelativePosition.Before, typeof(IncomingOutboxTransactionStep));
            }
            return result;
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

        // Only register the concrete handler after validation succeeds.
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
