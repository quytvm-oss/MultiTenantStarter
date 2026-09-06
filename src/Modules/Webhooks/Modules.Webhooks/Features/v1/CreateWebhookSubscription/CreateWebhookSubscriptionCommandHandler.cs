using Mediator;

using Modules.Webhooks.Contracts.v1;
using Modules.Webhooks.Data;
using Modules.Webhooks.Domain;
using Modules.Webhooks.Services;

namespace Modules.Webhooks.Features.v1.CreateWebhookSubscription;

public class CreateWebhookSubscriptionCommandHandler : ICommandHandler<CreateWebhookSubscriptionCommand, Guid>
{
    private readonly WebhookDbContext _dbContext;
    private readonly IWebhookSecretProtector _secretProtector;

    public CreateWebhookSubscriptionCommandHandler(WebhookDbContext dbContext, IWebhookSecretProtector secretProtector)
    {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
    }

    public async ValueTask<Guid> Handle(CreateWebhookSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Encrypt the signing secret at rest — it is the HMAC key, so it must be recoverable
        // (not hashed). Decrypted only at dispatch time to sign the outbound payload.
        var protectedSecret = _secretProtector.Protect(command.Secret);
        var subscription = WebhookSubscription.Create(command.Url, command.Events, protectedSecret);
        _dbContext.WebhookSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return subscription.Id;
    }

}
