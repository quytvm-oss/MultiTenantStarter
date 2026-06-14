using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

using Shared.Persistence;

namespace Persistence;

public class ConnectionStringValidator : IConnectionStringValidator
{
    private readonly DatabaseOptions _dbSettings;
    private readonly ILogger<ConnectionStringValidator> _logger;

    public ConnectionStringValidator(IOptions<DatabaseOptions> dbSettings, ILogger<ConnectionStringValidator> logger)
    {
        _dbSettings = dbSettings.Value;
        _logger = logger;
    }

    public bool TryValidate(string connectionString, string? dbProvider = null)
    {
        if (string.IsNullOrWhiteSpace(dbProvider))
        {
            dbProvider = _dbSettings.Provider;
        }

        try
        {
            switch (dbProvider.ToUpperInvariant())
            {
                case DbProviders.PostgreSQL:
                    _ = new NpgsqlConnectionStringBuilder(connectionString);
                    break;
                case DbProviders.MSSQL:
                    _ = new SqlConnectionStringBuilder(connectionString);
                    break;
                default:
                    break;
            }

            return true;
        }
        catch (ArgumentException e)
        {
            // Catches invalid connection string format from both NpgsqlConnectionStringBuilder
            // and SqlConnectionStringBuilder (both throw ArgumentException for malformed strings).
            _logger.LogError(e, "Connection String Validation Exception : {Error}", e.Message);
            return false;
        }
        catch (FormatException e)
        {
            // Catches format-related parsing failures in connection string values.
            _logger.LogError(e, "Connection String Validation Exception : {Error}", e.Message);
            return false;
        }
    }
}