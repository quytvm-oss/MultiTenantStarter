using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Persistence;

namespace Messaging.Persistence;

public sealed class MessagingDbInitializer : IDbInitializer
{
    private readonly MessagingDbContext _dbContext;
    private readonly ILogger<MessagingDbInitializer> _logger;
    public MessagingDbInitializer(MessagingDbContext dbContext, ILogger<MessagingDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("[Messaging] applied migrations");
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
