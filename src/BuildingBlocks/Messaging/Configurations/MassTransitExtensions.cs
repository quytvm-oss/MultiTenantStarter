using System.Reflection;

using MassTransit;

using Messaging.Abstractions;
using Messaging.Persistence;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Persistence;

namespace Messaging.Configurations;

public static class MassTransitExtensions
{

    public static IServiceCollection AddAppMassTransit(this IServiceCollection services,IConfiguration configuration,
        params Assembly[] assemblies)
    {

        // Register RabbitMQ Options
        services.AddOptions<RabbitMqOptions>().Bind(configuration.GetSection(RabbitMqOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddCustomDbContext<MessagingDbContext>();
        services.AddScoped<IDbInitializer, MessagingDbInitializer>();

        // Scan modules
        var modules = assemblies
            .Distinct()
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                typeof(IMassTransitModule).IsAssignableFrom(t)
                && t.IsClass
                && !t.IsAbstract)
            .Select(t => (IMassTransitModule)Activator.CreateInstance(t)!)
            .ToArray();

        // Register MassTransit
        services.AddMassTransit(x =>
        {
            // Register module consumers
            foreach (var module in modules)
            {
                module.RegisterConsumers(x);
            }
            x.AddEntityFrameworkOutbox<MessagingDbContext>(o =>
            {
                o.UsePostgres(false);
                o.UseBusOutbox();
                o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
            });
            x.AddDelayedMessageScheduler();

            x.UsingRabbitMq((context, cfg) =>
            {
                var options = context
                    .GetRequiredService<IOptions<RabbitMqOptions>>()
                    .Value;

                // Configure RabbitMQ connection
                cfg.ConfigureRabbitMqHost(options);
                cfg.UseDelayedMessageScheduler();

                // Configure module endpoints
                foreach (var module in modules)
                {
                    module.ConfigureEndpoints(cfg, context);
                }
            });
        });

        return services;
    }

    private static void ConfigureRabbitMqHost(
        this IRabbitMqBusFactoryConfigurator cfg,
        RabbitMqOptions options)
    {
        cfg.Host(
            options.Host,
            options.Port,
            options.VirtualHost,
            h =>
            {
                h.Username(options.Username);
                h.Password(options.Password);
            });
    }
}
