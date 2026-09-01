using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Webhooks.Domain;

namespace Modules.Webhooks.Data.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Deliveries", "webhooks");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(256);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(4096);
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasIndex(x => x.AttemptedAtUtc);
    }

}
