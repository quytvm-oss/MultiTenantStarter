namespace MessageBus.Persistence;

public interface IStorageInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);

    string GetPublishedTableName();

    string GetReceivedTableName();

    string GetLockTableName();
}