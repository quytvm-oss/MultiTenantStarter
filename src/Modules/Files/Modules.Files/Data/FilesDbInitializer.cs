using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Persistence;

namespace Modules.Files.Data;

public class FilesDbInitializer(FilesDbContext dbContext, ILogger<FilesDbInitializer> logger)
    : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Files] applied migrations");
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken)
    => Task.CompletedTask;
}