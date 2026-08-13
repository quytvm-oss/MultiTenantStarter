using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Modules.Files.Domain;

using Persistence.Context;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Files.Data;

public sealed class FilesDbContext(
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor, 
    DbContextOptions<FilesDbContext> options, 
    IOptions<DatabaseOptions> settings, 
    IHostEnvironment environment) : EfBaseDbContext(multiTenantContextAccessor, options, settings, environment)
{
    public DbSet<FileAsset>  FileAssets => Set<FileAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(FilesModuleConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilesDbContext).Assembly);
        // base.OnModelCreating runs LAST so BaseDbContext's auto-apply sees
        // fully-configured entities (including HasMany child types).
        base.OnModelCreating(modelBuilder);
    }
}