using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Modules.Webhooks.Domain;

using Persistence.Context;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Webhooks.Data;

public class WebhookDbContext(
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    DbContextOptions<WebhookDbContext> options,
    IOptions<DatabaseOptions> settings,
    IHostEnvironment environment) : EfBaseDbContext(multiTenantContextAccessor, options, settings, environment)
{
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("webhooks");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhookDbContext).Assembly);
        // base.OnModelCreating runs LAST so BaseDbContext's auto-apply sees
        // fully-configured entities (including HasMany child types).
        base.OnModelCreating(modelBuilder);
    }
}
