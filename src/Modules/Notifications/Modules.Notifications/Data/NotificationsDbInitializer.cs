using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Persistence;

namespace Modules.Notifications.Data;

public class NotificationsDbInitializer(NotificationsDbContext dbContext, ILogger<NotificationsDbInitializer> logger)
    : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Notifications] applied migrations");
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}