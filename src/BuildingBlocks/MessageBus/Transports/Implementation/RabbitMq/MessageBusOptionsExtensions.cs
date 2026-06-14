namespace MessageBus.Transports.Implementation.RabbitMq;

public static class MessageBusOptionsExtensions
{
    public static MessageBusOptions UseRabbitMQ(this MessageBusOptions options, string hostName)
    {
        return options.UseRabbitMQ(opt => { opt.HostName = hostName; });
    }

    // ReSharper disable once InconsistentNaming
    public static MessageBusOptions UseRabbitMQ(this MessageBusOptions options, Action<RabbitMQOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        options.RegisterExtension(new RabbitMqMessageBusOptionsExtension(configure));

        return options;
    }
}