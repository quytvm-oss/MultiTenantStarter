namespace MessageBus.Persistence.Implementation.PostgreSql;

public static class MessageBusOptionsExtensions
{
    public static MessageBusOptions UsePostgreSql(this MessageBusOptions options, string connectionString)
    {
        return options.UsePostgreSql(opt => { opt.ConnectionString = connectionString; });
    }

    private static MessageBusOptions UsePostgreSql(this MessageBusOptions options, Action<PostgreSqlOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        options.RegisterExtension(new PostgreSqlMsgOptionsExtension(configure));

        return options;
    }
}