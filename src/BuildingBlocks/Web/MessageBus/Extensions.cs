using System.Reflection;

using MessageBus;
using MessageBus.Persistence.Implementation.PostgreSql;
using MessageBus.Transports.Implementation.RabbitMq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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
                    rabbit.Password = "";
                    rabbit.ExchangeName = "messagebus";
                })
                .AddConsumerRegistrationsFromAssemblies(assemblies);
        });

        return builder;
    }
}