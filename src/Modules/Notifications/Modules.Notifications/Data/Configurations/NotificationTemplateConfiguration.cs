using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Notifications.Domain;

namespace Modules.Notifications.Data.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        builder.Property(x => x.Title).HasMaxLength(256);
        builder.Property(x => x.Subject).HasMaxLength(256);
        
        builder.Property(x => x.NotificationType) 
            .HasConversion(x => x.ToString(), x => Enum.Parse<NotificationType>(x))
            .IsRequired();
    }
}