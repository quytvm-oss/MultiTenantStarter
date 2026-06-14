using Npgsql;

namespace MessageBus.Persistence.Implementation.PostgreSql;

public class PostgreSqlOptions
{
    public const string DefaultSchema = "message";
    
    public string Schema { get; set; } = DefaultSchema;
    
    public string PublishName { get; set; } = "OutboxPublished";
    
    public string ReceivedName { get; set; } = "InboxReceived";
    
    public string LockName { get; set; } = "Lock";
    
    /// <summary>
    /// Gets or sets the database's connection string that will be used to store database entities.
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Npgsql data source that will be used to store database entities.
    /// </summary>
    public NpgsqlDataSource? DataSource { get; set; }

    /// <summary>
    /// Creates an Npgsql connection from the configured data source.
    /// </summary>
    internal NpgsqlConnection CreateConnection()
    {
        return DataSource != null ? DataSource.CreateConnection() : new NpgsqlConnection(ConnectionString);
    }
}