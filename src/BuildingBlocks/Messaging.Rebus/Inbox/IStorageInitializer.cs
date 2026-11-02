namespace Messaging.Rebus.Inbox;

public interface IStorageInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
