using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;

using Microsoft.EntityFrameworkCore;

using Modules.Multitenancy.Domain;
using Modules.Multitenancy.Provisioning;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Data;

public class TenantDbContext : EFCoreStoreDbContext<AppTenantInfo>
{
    
    public const string Schema = "tenant";
    
    public TenantDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<TenantProvisioning> TenantProvisionings => Set<TenantProvisioning>();

    public DbSet<TenantProvisioningStep> TenantProvisioningSteps => Set<TenantProvisioningStep>();

    public DbSet<TenantTheme> TenantThemes => Set<TenantTheme>();

    public DbSet<TenantExpiryNotice> TenantExpiryNotices => Set<TenantExpiryNotice>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);
    }
}