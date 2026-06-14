using MessageBus.Constants;
using MessageBus.Model;

namespace MessageBus.Persistence;

public interface IDataStorage
{
    Task<bool> AcquireLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default);

    Task ReleaseLockAsync(string key, string instance, CancellationToken token = default);

    Task RenewLockAsync(string key, TimeSpan ttl, string instance, CancellationToken token = default);

    Task ChangePublishStateToDelayedAsync(string[] ids);

    Task ChangePublishStateAsync(MessageContext message, StatusName state, object? transaction = null);

    Task ChangeReceiveStateAsync(MessageContext message, StatusName state);

    Task<MessageContext> StoreMessageAsync(string name, Message content, object? transaction = null);

    Task StoreReceivedExceptionMessageAsync(string name, string group, string content);

    Task<MessageContext> StoreReceivedMessageAsync(string name, string group, Message content);

    Task<int> DeleteExpiresAsync(string table, DateTime timeout, int batchCount = 1000, CancellationToken token = default);

    Task<IEnumerable<MessageContext>> GetPublishedMessagesOfNeedRetry(TimeSpan lookbackSeconds);

    Task ScheduleMessagesOfDelayedAsync(Func<object, IEnumerable<MessageContext>, Task> scheduleTask, CancellationToken token = default);

    Task<IEnumerable<MessageContext>> GetReceivedMessagesOfNeedRetry(TimeSpan lookbackSeconds);

    Task<int> DeleteReceivedMessageAsync(long id);

    Task<int> DeletePublishedMessageAsync(long id);
}