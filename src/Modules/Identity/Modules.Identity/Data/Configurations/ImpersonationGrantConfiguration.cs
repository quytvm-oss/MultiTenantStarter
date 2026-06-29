using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Identity.Domain;

namespace Modules.Identity.Data.Configurations;

public class ImpersonationGrantConfiguration : IEntityTypeConfiguration<ImpersonationGrant>
{
    public void Configure(EntityTypeBuilder<ImpersonationGrant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ImpersonationGrants", IdentityModuleConstants.SchemaName);
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Jit).IsRequired().HasMaxLength(64);
        
        builder.HasIndex(x => x.Jit).IsUnique();
        
        builder.Property(x => x.ActorTenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ActorUserName).HasMaxLength(256);
        builder.Property(x => x.ActorTenantId).IsRequired().HasMaxLength(64);
        
        builder.Property(x => x.ImpersonatedUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ImpersonatedUserName).HasMaxLength(256);
        builder.Property(x => x.ImpersonatedTenantId).IsRequired().HasMaxLength(64);
        
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        builder.Property(x => x.RevokeReason).HasMaxLength(500);
        builder.Property(x => x.RevokedByUserId).HasMaxLength(64);
        builder.Property(x => x.RevokedByUserName).HasMaxLength(256);
        
        builder.Property(x => x.ClientId).HasMaxLength(128);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        
        // Composite index supports the most common query: "active grants in
        // tenant X, newest first".
        builder.HasIndex(x => new {x.ImpersonatedTenantId, x.StartedAtUtc})
            .HasDatabaseName("IX_ImpersonationGrants_ImpersonatedTenantId_StartedAtUtc");
        
        builder.HasIndex(x => new {x.ActorUserId, x.StartedAtUtc})
            .HasDatabaseName("IX_ImpersonationGrants_ActorUserId_StartedAtUtc");
    }
}