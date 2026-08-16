using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Modules.Notifications.Domain;

using Persistence.Context;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Notifications.Data;

public class NotificationsDbContext(
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor, 
    DbContextOptions<NotificationsDbContext> options, 
    IOptions<DatabaseOptions> settings, 
    IHostEnvironment environment) : EfBaseDbContext(multiTenantContextAccessor, options, settings, environment)
{
    
    public DbSet<Notification> Notifications => Set<Notification>();
    
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(NotificationModuleConstant.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        // base.OnModelCreating runs LAST so BaseDbContext's auto-apply (ApplyTenantIsolationByDefault)
        // sees fully-configured entities, including child types reached via HasMany navigation.
        base.OnModelCreating(modelBuilder);
    }
}