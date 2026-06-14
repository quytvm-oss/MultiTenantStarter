using System.Data;
using System.Data.Common;

namespace MessageBus.Persistence.Implementation.PostgreSql;

internal static class DbConnectionExtensions
{
    public static async Task<int> ExecuteNonQueryAsync(this DbConnection connection, string sql, DbTransaction? transaction = null, 
        params DbParameter[] sqlParams)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync().ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql, transaction, sqlParams);
        return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static async Task<T> ExecuteReaderAsync<T>(this DbConnection connection, string sql, Func<DbDataReader, Task<T>> readerFunc, 
        DbTransaction? transaction = null, params DbParameter[] sqlParams)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync().ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql, transaction, sqlParams);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

        return await readerFunc(reader).ConfigureAwait(false);
    }

    public static async Task<T> ExecuteScalarAsync<T>(this DbConnection connection, string sql, DbTransaction? transaction = null, 
        params DbParameter[] sqlParams)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync().ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql, transaction, sqlParams);

        var value = await command.ExecuteScalarAsync().ConfigureAwait(false);

        if (value is null or DBNull)
            return default!;

        return (T)Convert.ChangeType(value, typeof(T));
    }
    
    public static async Task<T?> ExecuteScalarAsync<T>(
        this DbConnection connection,
        string sql,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default,
        params DbParameter[] sqlParams)
    {
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(connection, sql, transaction, sqlParams);

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (value is null or DBNull)
            return default;

        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql, DbTransaction? transaction, DbParameter[] sqlParams)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = sql;
        command.Transaction = transaction;

        foreach (var param in sqlParams)
            command.Parameters.Add(param);

        return command;
    }
}