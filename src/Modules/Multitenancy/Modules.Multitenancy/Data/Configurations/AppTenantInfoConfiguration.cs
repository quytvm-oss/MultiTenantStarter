using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Data.Configurations;

public class AppTenantInfoConfiguration : IEntityTypeConfiguration<AppTenantInfo>
{
    public void Configure(EntityTypeBuilder<AppTenantInfo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Tenants", MultitenancyConstants.Schema);
        
        builder.HasKey(x => x.Id);
        
        // builder.Property(t => t.Plan).HasMaxLength(64);
    }
}