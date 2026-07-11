using Core.Domain;

using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Persistence.Context;

public class EfBaseDbContext(IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    DbContextOptions options,
    IOptions<DatabaseOptions> settings,
    IHostEnvironment environment)
    : MultiTenantDbContext(multiTenantContextAccessor, options)
{
    private readonly DatabaseOptions _settings = settings.Value;

    /// <summary>
    /// Configures the model and its relationships by applying global filters, tenant isolation,
    /// and other customization logic during the model creation stage of the database context.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> instance used to define the model
    /// configuration for the database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="modelBuilder"/> argument is null.</exception>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(QueryFilters.SoftDelete, s => !s.IsDeleted);
        base.OnModelCreating(modelBuilder);
        // Default-on tenant isolation: entities not marked IGlobalEntity get IsMultiTenant().
        // Subclasses must call base.OnModelCreating AFTER ApplyConfigurationsFromAssembly so per-entity configs are in place.
        modelBuilder.ApplyTenantIsolationByDefault();
    }

    /// <summary>
    /// Configures the database context with additional options such as the database provider,
    /// tenant-specific connection string, and migration assembly during the setup process.
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder"/> instance used to configure the database context options.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="optionsBuilder"/> argument is null.</exception>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        if (!string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext.TenantInfo?.ConnectionString))
        {
            optionsBuilder.ConfigureCustomDatabase(
                _settings.Provider,
                multiTenantContextAccessor.MultiTenantContext.TenantInfo.ConnectionString,
                _settings.MigrationsAssembly,
                environment.IsDevelopment());
        }
        
        //base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Saves the changes made in the current context asynchronously, applying tenant isolation
    /// and considering any multi-tenancy configurations before executing the save operation.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains
    /// the number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        TenantNotSetMode = TenantNotSetMode.Overwrite;
        int result = await  base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}