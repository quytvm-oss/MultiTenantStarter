using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Webhooks.Domain;

namespace Modules.Webhooks.Data.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Subscriptions", "webhooks");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.EventsCsv).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.ProtectedSecret).IsRequired();
        builder.HasIndex(x => x.IsActive);
    }

}
