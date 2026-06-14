using MessageBus.Constants;
using MessageBus.Contracts;
using MessageBus.Processors;
using MessageBus.Subscribes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MessageBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessageBus(this IServiceCollection services, Action<MessageBusOptions> setupAction)
    {
        var options = new MessageBusOptions();
        setupAction(options);
        services.Configure(setupAction);
        
       var builder = new SubscribeBusBuilder(
           services.TryAddScoped,
           new OptionsWrapper<MessageBusOptions>(options));

       foreach (var factory in options.ConsumerRegistrations)
       {
           var registration = factory();

           registration.Register(builder);
       }

       builder.Build();

       services.TryAddSingleton<ISubscribeBusBuilder>(builder);

        Snowflake.Configure(1);
        services.TryAddSingleton<SubscriptionMatcherCache>();
        services.TryAddSingleton<IPublisher, Publisher>();
        services.TryAddSingleton<IDispatcher, Dispatcher>();
        services.TryAddSingleton<IMessageSender, MessageSender>();
        services.TryAddSingleton<ISubscribeExecutor, SubscribeExecutor>();
        services.TryAddSingleton<IConsumerRegister, ConsumerRegister>();
        services.TryAddSingleton<ISerializer, Serializer>();

        services.TryAddSingleton<MessageRetryProcessor>();
        services.TryAddSingleton<TransportConsumerCheckProcessor>();
        services.TryAddSingleton<MessageDelayedProcessor>();
        services.TryAddSingleton<DeleteMessageExpiredProcessor>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, IDispatcher>(
            sp => sp.GetRequiredService<IDispatcher>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, IConsumerRegister>(
            sp => sp.GetRequiredService<IConsumerRegister>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, DefaultProcessingServer>());

        foreach (var extension in options.Extensions)
            extension.AddExtendServices(services);

        services.AddSingleton<BusBackgroundService>();
        services.AddHostedService(sp => sp.GetRequiredService<BusBackgroundService>());

        return services;
    }
    
    public static MessageBusOptions AddConsumerRegistration<T>(
        this MessageBusOptions options)
        where T : class, IConsumerRegistration, new()
    {
        options.ConsumerRegistrations.Add(static () => new T());

        return options;
    }
}