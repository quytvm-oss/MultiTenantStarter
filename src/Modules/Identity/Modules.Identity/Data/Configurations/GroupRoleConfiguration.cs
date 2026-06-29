using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Identity.Domain;

namespace Modules.Identity.Data.Configurations;

public class GroupRoleConfiguration : IEntityTypeConfiguration<GroupRole>
{
    public void Configure(EntityTypeBuilder<GroupRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("GroupRoles", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
        
        builder.HasKey(x => new { x.GroupId, x.RoleId });

        builder.Property(gr => gr.RoleId).IsRequired().HasMaxLength(450);
        
        builder.HasOne(gr => gr.Group)
            .WithMany(g => g.GroupRoles)
            .HasForeignKey(gr => gr.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(gr => gr.Role)
            .WithMany()
            .HasForeignKey(gr => gr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.RoleId);
    }
}