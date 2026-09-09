using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;
using Modules.Webhooks.Data;

namespace Modules.Webhooks.Features.v1.DeleteWebhookSubscription;

public class DeleteWebhookSubscriptionCommandHandler : ICommandHandler<DeleteWebhookSubscriptionCommand>
{
    private readonly WebhookDbContext _dbContext;

    public DeleteWebhookSubscriptionCommandHandler(WebhookDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async ValueTask<Unit> Handle(DeleteWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subscription = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Webhook subscription {command.Id} not found.");

        _dbContext.WebhookSubscriptions.Remove(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }

}
