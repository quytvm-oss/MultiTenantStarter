using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Notifications.Domain;

namespace Modules.Notifications.Data.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        builder.Property(x => x.Title).HasMaxLength(256);
        builder.Property(x => x.Subject).HasMaxLength(256);
        
        builder.Property(x => x.Type) 
            .HasConversion(x => x.ToString(), x => Enum.Parse<EmailTemplateType>(x))
            .IsRequired();
    }
}