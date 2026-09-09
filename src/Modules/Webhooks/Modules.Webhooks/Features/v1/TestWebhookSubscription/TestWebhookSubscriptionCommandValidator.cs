using FluentValidation;

using Modules.Webhooks.Contracts.v1.TestWebhookSubscription;

namespace Modules.Webhooks.Features.v1.TestWebhookSubscription;

public class TestWebhookSubscriptionCommandValidator : AbstractValidator<TestWebhookSubscriptionCommand>
{
    public TestWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
    }
}
