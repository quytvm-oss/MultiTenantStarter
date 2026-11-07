using System;
using System.Collections.Generic;
using System.Text;

using Finbuckle.MultiTenant.Abstractions;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Persistence.Context;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Messaging.Persistence;

public sealed class MessagingDbContext: DbContext
{
    public static string Schema = "messaging";

    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
