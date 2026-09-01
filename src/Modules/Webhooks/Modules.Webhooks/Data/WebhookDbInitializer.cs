using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Persistence;

namespace Modules.Webhooks.Data.Configurations;

public class WebhookDbInitializer(WebhookDbContext dbContext, ILogger<WebhookDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Webhooks] applied migrations");
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
