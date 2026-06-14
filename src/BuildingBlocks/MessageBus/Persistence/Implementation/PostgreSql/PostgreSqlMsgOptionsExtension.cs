using Microsoft.Extensions.DependencyInjection;

namespace MessageBus.Persistence.Implementation.PostgreSql;

public class PostgreSqlMsgOptionsExtension: IMessageBusOptionsExtension
{ 
    private readonly Action<PostgreSqlOptions> _configure;

    public PostgreSqlMsgOptionsExtension(Action<PostgreSqlOptions> configure)
    {
        _configure = configure;
    }

    public void AddExtendServices(IServiceCollection services)
    {
        services.AddSingleton<IDataStorage, PostgreSqlDataStorage>();
        services.AddSingleton<IStorageInitializer, PostgreSqlStorageInitializer>();
        services.Configure(_configure);
    }
}