using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Identity.Domain;

namespace Modules.Identity.Data.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UserGroups", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
        
        builder.HasKey(x => new { x.UserId, x.GroupId });
        
        builder.Property(ug => ug.UserId).IsRequired().HasMaxLength(450);
        
        builder.Property(x => x.AddedBy)
            .HasMaxLength(450);
        
        builder.Property(x => x.AddedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.HasOne(ug => ug.User)
            .WithMany()
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(ug => ug.Group)
            .WithMany()
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(ug => ug.UserId);
        builder.HasIndex(ug => ug.GroupId);
    }
}