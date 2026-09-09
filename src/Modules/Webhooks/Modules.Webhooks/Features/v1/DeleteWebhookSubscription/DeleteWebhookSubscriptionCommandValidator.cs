using FluentValidation;

using Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;

namespace Modules.Webhooks.Features.v1.DeleteWebhookSubscription;

public class DeleteWebhookSubscriptionCommandValidator : AbstractValidator<DeleteWebhookSubscriptionCommand>
{
    public DeleteWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Webhook subscription ID is required.");
    }
}
