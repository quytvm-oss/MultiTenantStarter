using Core.Domain;

using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Persistence;

public static class TenantIsolationExtensions
{
    private const string FinbuckleMultiTenantAnnotation = "Finbuckle:MultiTenant";

    public static void ApplyTenantIsolationByDefault(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;

            if (entityType.FindPrimaryKey() is null) continue;
            if (typeof(IGlobalEntity).IsAssignableTo(entityType.ClrType)) continue;
            if (entityType.FindAnnotation(FinbuckleMultiTenantAnnotation) is not null) continue;

            modelBuilder.Entity(entityType.ClrType).IsMultiTenant().AdjustUniqueIndexes();
        }
    }
}