using System.Data.Common;

using MessageBus.Constants;
using MessageBus.Contracts;
using MessageBus.Model;

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

using Npgsql;

namespace MessageBus.Persistence.Implementation.PostgreSql;

public class PostgreSqlDataStorage : IDataStorage
{
    
    private readonly IOptions<MessageBusOptions> _mesOptions;
    private readonly IOptions<PostgreSqlOptions> _options;
    private readonly ISerializer _serializer;
    private readonly string _pubName;
    private readonly string _recName;
    private readonly string _lockName;

    public PostgreSqlDataStorage(
        IOptions<MessageBusOptions> msgOptions,
        IOptions<PostgreSqlOptions> options,
        IStorageInitializer initializer,
        ISerializer serializer)
    {
        _mesOptions = msgOptions;
        _options = options;
        _serializer = serializer;
        _pubName = initializer.GetPublishedTableName();
        _recName = initializer.GetReceivedTableName();
        _lockName = initializer.GetLockTableName();
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance,
        CancellationToken token = default)
    {
        var sql =
            $"UPDATE {_lockName} SET \"Instance\"=@Instance,\"LastLockTime\"=@LastLockTime WHERE \"Key\"=@Key AND \"LastLockTime\" < @TTL;";
        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        DbParameter[] sqlParams =
        {
            new NpgsqlParameter("@Instance", instance),
            new NpgsqlParameter("@LastLockTime", DateTime.UtcNow),
            new NpgsqlParameter("@Key", key),
            new NpgsqlParameter("@TTL", DateTime.UtcNow.Subtract(ttl))
        };
        var opResult = await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        return opResult > 0;
    }

    public async Task ReleaseLockAsync(string key, string instance, CancellationToken token = default)
    {
        var sql =
            $"UPDATE {_lockName} SET \"Instance\"='',\"LastLockTime\"=@LastLockTime WHERE \"Key\"=@Key AND \"Instance\"=@Instance;";
        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        DbParameter[] sqlParams =
        [
            new NpgsqlParameter("@Instance", instance),
            new NpgsqlParameter("@LastLockTime", DateTime.MinValue),
            new NpgsqlParameter("@Key", key)
        ];
        await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
    }

    public async Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default)
    {
        var sql =
            $"UPDATE {_lockName} SET \"LastLockTime\"=\"LastLockTime\"+interval '{ttl.TotalSeconds}' second WHERE \"Key\"=@Key AND \"Instance\"=@Instance;";
        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        DbParameter[] sqlParams =
        [
            new NpgsqlParameter("@Instance", instance),
            new NpgsqlParameter("@Key", key)
        ];
        await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
    }

    public async Task ChangePublishStateToDelayedAsync(string[] ids)
    {
        var sql =
            $"UPDATE {_pubName} SET \"StatusName\"='{StatusName.Delayed}' WHERE \"Id\" IN ({string.Join(',', ids)});";
        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        await connection.ExecuteNonQueryAsync(sql).ConfigureAwait(false);
    }

    public async Task ChangePublishStateAsync(MessageContext message, StatusName state, object? transaction = null)
    {
        await ChangeMessageStateAsync(_pubName, message, state, transaction).ConfigureAwait(false);
    }

    public async Task ChangeReceiveStateAsync(MessageContext message, StatusName state)
    {
        await ChangeMessageStateAsync(_recName, message, state).ConfigureAwait(false);
    }

    public async Task<MessageContext> StoreMessageAsync(string name, Message content, object? transaction = null)
    {
        var sql =
            $"INSERT INTO {_pubName} (\"Id\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
            $"VALUES(@Id,@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

        var message = new MessageContext
        {
            DbId = content.GetId(),
            Origin = content,
            Content = _serializer.Serialize(content),
            Added = DateTime.UtcNow,
            ExpiresAt = null,
            Retries = 0
        };

        DbParameter[] sqlParams =
        [
            new NpgsqlParameter("@Id", long.Parse(message.DbId)),
            new NpgsqlParameter("@Name", name),
            new NpgsqlParameter("@Content", message.Content),
            new NpgsqlParameter("@Retries", message.Retries),
            new NpgsqlParameter("@Added", message.Added),
            new NpgsqlParameter("@ExpiresAt", message.ExpiresAt.HasValue ? message.ExpiresAt.Value : DBNull.Value),
            new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
        ];

        
        if (transaction == null)
        {
            var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }
        else
        {
            var dbTrans = transaction as DbTransaction;
            if (dbTrans == null && transaction is IDbContextTransaction dbContextTrans)
                dbTrans = dbContextTrans.GetDbTransaction();

            var conn = dbTrans?.Connection!;
            await conn.ExecuteNonQueryAsync(sql, dbTrans, sqlParams).ConfigureAwait(false);
        }

        return message;
    }

    public async Task StoreReceivedExceptionMessageAsync(string name, string group, string content)
    {
        DbParameter[] sqlParams =
        [
            new NpgsqlParameter("@Id", Snowflake.NewId()),
            new NpgsqlParameter("@Name", name),
            new NpgsqlParameter("@Group", group),
            new NpgsqlParameter("@Content", content),
            new NpgsqlParameter("@Retries", _mesOptions.Value.FailedRetryCount),
            new NpgsqlParameter("@Added", DateTime.UtcNow),
            new NpgsqlParameter("@ExpiresAt", DateTime.UtcNow.AddSeconds(_mesOptions.Value.FailedMessageExpiredAfter)),
            new NpgsqlParameter("@StatusName", nameof(StatusName.Failed))
        ];

        await StoreReceivedMessage(sqlParams).ConfigureAwait(false);
    }

    public async Task<MessageContext> StoreReceivedMessageAsync(string name, string group, Message message)
    {
        var mdMessage = new MessageContext
        {
            DbId = Snowflake.NewId().ToString(),
            Origin = message,
            Added = DateTime.UtcNow,
            ExpiresAt = null,
            Retries = 0
        };

        DbParameter[] sqlParams =
        [
            new NpgsqlParameter("@Id", long.Parse(mdMessage.DbId)),
            new NpgsqlParameter("@Name", name),
            new NpgsqlParameter("@Group", group),
            new NpgsqlParameter("@Content", _serializer.Serialize(mdMessage.Origin)),
            new NpgsqlParameter("@Retries", mdMessage.Retries),
            new NpgsqlParameter("@Added", mdMessage.Added),
            new NpgsqlParameter("@ExpiresAt", mdMessage.ExpiresAt.HasValue ? mdMessage.ExpiresAt.Value : DBNull.Value),
            new NpgsqlParameter("@StatusName", nameof(StatusName.Scheduled))
        ];

        await StoreReceivedMessage(sqlParams).ConfigureAwait(false);

        return mdMessage;
    }

    public async Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000,
        CancellationToken token = default)
    {
        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);

        return await connection.ExecuteNonQueryAsync(
            $@"DELETE FROM {table}
               WHERE ""Id"" IN (
                   SELECT ""Id""
                   FROM {table}
                   WHERE ""ExpiresAt"" < @timeout
                   AND ""StatusName"" IN ('{StatusName.Succeeded}','{StatusName.Failed}')
                   LIMIT @batchCount
               )",
             null,
             new NpgsqlParameter("@timeout", timeout),
             new NpgsqlParameter("@batchCount", batchCount)).ConfigureAwait(false);
    }

    public async Task<IEnumerable<MessageContext>> GetPublishedMessagesOfNeedRetry(TimeSpan lookbackSeconds)
    {
        return await GetMessagesOfNeedRetryAsync(_pubName, lookbackSeconds).ConfigureAwait(false);
    }

    public async Task<IEnumerable<MessageContext>> GetReceivedMessagesOfNeedRetry(TimeSpan lookbackSeconds)
    {
        return await GetMessagesOfNeedRetryAsync(_recName, lookbackSeconds).ConfigureAwait(false);
    }

    public async Task<int> DeleteReceivedMessageAsync(long id)
    {
        var sql = $@"DELETE FROM {_recName} WHERE ""Id""={id}";

        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        var result = await connection.ExecuteNonQueryAsync(sql);
        return result;
    }

    public async Task<int> DeletePublishedMessageAsync(long id)
    {
        var sql = $@"DELETE FROM {_pubName} WHERE ""Id""={id}";

        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        var result = await connection.ExecuteNonQueryAsync(sql);
        return result;
    }

    public async Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MessageContext>, Task> scheduleTask,
        CancellationToken token = default)
    {
        var sql =
            $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\" FROM {_pubName} WHERE " +
            $" ((\"ExpiresAt\"< @TwoMinutesLater AND \"StatusName\" = '{StatusName.Delayed}') OR (\"ExpiresAt\"< @OneMinutesAgo AND \"StatusName\" = '{StatusName.Queued}')) FOR UPDATE SKIP LOCKED LIMIT @BatchSize;";

        DbParameter[]  sqlParams =
        [
            new NpgsqlParameter("@TwoMinutesLater", DateTime.UtcNow.AddMinutes(2)),
            new NpgsqlParameter("@OneMinutesAgo", QueuedMessageFetchTime()),
            new NpgsqlParameter("@BatchSize", _mesOptions.Value.SchedulerBatchSize)
        ];

        await using var connection = _options.Value.CreateConnection();
        await connection.OpenAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(token);
        var messageList = await connection.ExecuteReaderAsync(sql, async reader =>
        {
            var messages = new List<MessageContext>();
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                messages.Add(new MessageContext
                {
                    DbId = reader.GetInt64(0).ToString(),
                    Origin = _serializer.Deserialize(reader.GetString(1))!,
                    Retries = reader.GetInt32(2),
                    Added = reader.GetDateTime(3),
                    ExpiresAt = reader.GetDateTime(4)
                });
            }

            return messages;
        }, transaction, sqlParams).ConfigureAwait(false);

        await scheduleTask(transaction, messageList);

        await transaction.CommitAsync(token);
    }

    protected virtual DateTime QueuedMessageFetchTime()
    {
        return DateTime.UtcNow.AddMinutes(-1);
    }

    private async Task ChangeMessageStateAsync(string tableName, MessageContext message, StatusName state,
        object? transaction = null)
    {
        var sql =
            $"UPDATE {tableName} SET \"Content\"=@Content,\"Retries\"=@Retries,\"ExpiresAt\"=@ExpiresAt,\"StatusName\"=@StatusName WHERE \"Id\"=@Id";

        DbParameter[]  sqlParams =
        [
            new NpgsqlParameter("@Id", long.Parse(message.DbId)),
            new NpgsqlParameter("@Content", _serializer.Serialize(message.Origin)),
            new NpgsqlParameter("@Retries", message.Retries),
            new NpgsqlParameter("@ExpiresAt", message.ExpiresAt),
            new NpgsqlParameter("@StatusName", state.ToString("G"))
        ];

        if (transaction is DbTransaction dbTransaction)
        {
            var connection = (NpgsqlConnection)dbTransaction.Connection!;
            await connection.ExecuteNonQueryAsync(sql, dbTransaction, sqlParams).ConfigureAwait(false);
        }
        else
        {
            await using var connection = _options.Value.CreateConnection();
            await using var _ = connection.ConfigureAwait(false);
            await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
        }
    }

    private async Task StoreReceivedMessage(DbParameter[] sqlParams)
    {
        var sql =
            $"INSERT INTO {_recName}(\"Id\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")" +
            $"VALUES(@Id,@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";

        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        await connection.ExecuteNonQueryAsync(sql, sqlParams: sqlParams).ConfigureAwait(false);
    }

    private async Task<IEnumerable<MessageContext>> GetMessagesOfNeedRetryAsync(string tableName, TimeSpan lookbackSeconds)
    {
        var fourMinAgo = DateTime.UtcNow.Subtract(lookbackSeconds);
        var sql =
            $"SELECT \"Id\",\"Content\",\"Retries\",\"Added\" FROM {tableName} WHERE \"Retries\"<@Retries " +
            $"AND \"Added\"<@Added AND \"StatusName\" IN ('{StatusName.Failed}','{StatusName.Scheduled}') LIMIT 200;";

        DbParameter[] sqlParams =
        [
            new NpgsqlParameter("@Retries", _mesOptions.Value.FailedRetryCount),
            new NpgsqlParameter("@Added", fourMinAgo)
        ];

        var connection = _options.Value.CreateConnection();
        await using var _ = connection.ConfigureAwait(false);
        var result = await connection.ExecuteReaderAsync(sql, async reader =>
        {
            var messages = new List<MessageContext>();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                messages.Add(new MessageContext
                {
                    DbId = reader.GetInt64(0).ToString(),
                    Origin = _serializer.Deserialize(reader.GetString(1))!,
                    Retries = reader.GetInt32(2),
                    Added = reader.GetDateTime(3)
                });
            }

            return messages;
        }, sqlParams: sqlParams).ConfigureAwait(false);

        return result;
    }
    
    private DbConnection CreateConnection()
        => new NpgsqlConnection(_options.Value.ConnectionString);
}