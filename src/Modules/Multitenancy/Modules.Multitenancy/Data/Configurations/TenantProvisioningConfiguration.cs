using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Multitenancy.Provisioning;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Data.Configurations;

public class TenantProvisioningConfiguration : IEntityTypeConfiguration<TenantProvisioning>
{
    public void Configure(EntityTypeBuilder<TenantProvisioning> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TenantProvisionings", MultitenancyConstants.Schema);
        
        builder.HasMany(x => x.Steps)
            .WithOne(s => s.Provisioning!)
            .HasForeignKey(s => s.ProvisioningId);
    }
}