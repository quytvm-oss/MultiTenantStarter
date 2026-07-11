using System.Linq.Expressions;
using System.Reflection;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Persistence.Context;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Auditing.Persistence;

public sealed class AuditDbContext(
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    DbContextOptions<AuditDbContext> options,
    IOptions<DatabaseOptions> settings,
    IHostEnvironment environment)
    : EfBaseDbContext(multiTenantContextAccessor, options, settings, environment)
{
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        
        // Required for the trigram GIN indexes on Source/UserName. Idempotent (IF NOT EXISTS); the
        // migration role needs CREATE permission on the database.
        modelBuilder.HasPostgresExtension("pg_trgm");
        
        
        // Map AuditJsonbFunctions.AsText to `CAST(x AS text)` so jsonb PayloadJson is ILIKE-searchable.
        // Without the cast, ILIKE on jsonb throws ("like_escape(jsonb, unknown) does not exist") → HTTP 500.
        var textMapping = this.GetService<IRelationalTypeMappingSource>().FindMapping(typeof(string))!;
        var asTextMethod = typeof(AuditJsonbFunctions)
            .GetMethod(nameof(AuditJsonbFunctions.AsText), BindingFlags.Public | BindingFlags.Static)!;
        modelBuilder
            .HasDbFunction(asTextMethod)
            .HasTranslation(args => new SqlUnaryExpression(
                ExpressionType.Convert,
                args[0],
                typeof(string),
                textMapping));
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}