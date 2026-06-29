using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Identity.Domain;

namespace Modules.Identity.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UserSessions", IdentityModuleConstants.SchemaName)
            .HasKey(x => x.Id);
        
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(256);
        
        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(s => s.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(s => s.UserAgent)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(s => s.DeviceType)
            .HasMaxLength(50);

        builder.Property(s => s.Browser)
            .HasMaxLength(100);

        builder.Property(s => s.BrowserVersion)
            .HasMaxLength(50);

        builder.Property(s => s.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(s => s.OsVersion)
            .HasMaxLength(50);

        builder.Property(s => s.RevokedBy)
            .HasMaxLength(450);

        builder.Property(s => s.RevokedReason)
            .HasMaxLength(500);
        
        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.RefreshTokenHash);
        builder.HasIndex(s => s.ExpiresAt);
        builder.HasIndex(s => new { s.UserId, s.IsRevoked });
    }
}