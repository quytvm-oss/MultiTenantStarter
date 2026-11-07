using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Modules.Identity.Data;
using Modules.Webhooks.Contracts.Events;

namespace Modules.Identity.Events;

public class WebhookEventHandler(ILogger<WebhookEventHandler> logger, IdentityDbContext db) : IConsumer<WebhookEvent>
{
    public async Task Consume(ConsumeContext<WebhookEvent> context)
    {
        var message = context.Message;
        var connectionString = db.Database.GetConnectionString();

        logger.LogInformation(
            "Processing WebhookEvent: TenantId={TenantId}, UserId={UserId}, Database={Database}",
            message.TenantId,
            message.UserId,
            db.Database.GetDbConnection().Database);

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.UserId, CancellationToken.None);

        if (user is null)
        {
            logger.LogWarning(
                "User not found. TenantId={TenantId}, UserId={UserId}",
                message.TenantId,
                message.UserId);
            return;
        }

        logger.LogInformation(
            "User found. TenantId={TenantId}, Database={Database}, UserId={UserId}, UserName={UserName}, Email={Email}",
            message.TenantId,
            db.Database.GetDbConnection().Database,
            user.Id,
            user.UserName,
            user.Email);
    }

}
