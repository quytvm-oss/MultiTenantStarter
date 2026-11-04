namespace Messaging.Rebus.Inbox;

public interface IInboxStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<InboxAcquireResult> TryAcquireAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default);
    Task<bool> RenewAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default);
    Task<bool> CompleteAsync(InboxKey key, Guid lockId, CancellationToken cancellationToken = default);
    Task FailAsync(InboxKey key, Guid lockId, string error, CancellationToken cancellationToken = default);
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}
