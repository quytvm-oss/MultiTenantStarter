using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Persistence;

namespace Modules.Auditing.Persistence;

public class AuditDbInitializer(AuditDbContext context, ILogger<AuditDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("[{Tenant}] applied database migrations for audit module", context.TenantInfo?.Identifier);
            }
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}