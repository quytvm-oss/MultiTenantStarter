using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Identity.Domain;

namespace Modules.Identity.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("Groups", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        
        builder.Property(x => x.Description).HasMaxLength(1024);
        
        builder.Property(x => x.CreatedBy).HasMaxLength(450);
        
        builder.Property(x => x.LastModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(450);

        builder.Property(x => x.DeletedBy).HasMaxLength(450);
        
        builder.Property(x => x.CreatedOnUtc).HasColumnName("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.Property(x => x.LastModifiedOnUtc).HasColumnName("ModifiedAt");
        
        // Indexes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsDefault);
    }
}