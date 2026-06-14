namespace Shared.Persistence;

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    
    public string Provider { get; set; } = DbProviders.PostgreSQL;
    
    public string MigrationAssembly { get; set; } = string.Empty;
}