using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Billing.Contracts;

using Modules.Billing.Domain;

using Persistence;

namespace Modules.Billing.Data;

public sealed class BillingDbInitializer : IDbInitializer
{
    private readonly BillingDbContext _dbContext;
    private readonly ILogger<BillingDbInitializer> _logger;
    public BillingDbInitializer(BillingDbContext dbContext, ILogger<BillingDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("[Billing] applied migrations");
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Plans are a global catalogue (IGlobalEntity); seed defaults once. "free" backs the trial
        // fallback; keys align with QuotaOptions plan keys so quota limits resolve.
        if (await _dbContext.Plans.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        _dbContext.Plans.Add(BillingPlan.Create("free", "Free", "USD", 0m, interval: PlanInterval.Monthly));
        _dbContext.Plans.Add(BillingPlan.Create("pro", "Pro", "USD", 29m, interval: PlanInterval.Monthly));
        _dbContext.Plans.Add(BillingPlan.Create("pro-annual", "Pro (Annual)", "USD", 29m,
            interval: PlanInterval.Yearly, annualPrice: 290m));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("[Billing] seeded default plans (free, pro, pro-annual)");
    }

}
