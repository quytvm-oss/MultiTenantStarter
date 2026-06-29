using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Identity.Domain;

namespace Modules.Identity.Data.Configurations;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PasswordHistory", IdentityModuleConstants.SchemaName)
            .HasKey(x => x.Id);
        
        builder.Property(x => x.PasswordHash).IsRequired();
        
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.HasOne(x => x.User)
            .WithMany(x => x.PasswordHistories)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId ,x.CreatedAt });
    }
}