namespace Persistence;

public interface IDbInitializer
{
    /// <summary>
    /// Executes the process of applying pending migrations to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the migration operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MigrateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Populates the database with initial data asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the seeding operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SeedAsync(CancellationToken cancellationToken);
}