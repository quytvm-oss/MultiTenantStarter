using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Multitenancy.Domain;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Data.Configurations;

public class TenantExpiryNoticeConfiguration : IEntityTypeConfiguration<TenantExpiryNotice>
{
    public void Configure(EntityTypeBuilder<TenantExpiryNotice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TenantExpiryNotices", MultitenancyConstants.Schema);
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NoticeType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ValidUptoUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
    }
}