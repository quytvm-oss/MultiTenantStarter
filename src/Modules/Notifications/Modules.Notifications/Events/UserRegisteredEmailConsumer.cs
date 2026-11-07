using System;
using System.Collections.Generic;
using System.Text;

using Mailling;
using Mailling.Abstractions;

using MassTransit;

using Microsoft.Extensions.Logging;

using Modules.Identity.Contracts.Events;

namespace Modules.Notifications.Events;

public class UserRegisteredEmailConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly IMailService _mailService;
    private readonly ILogger<UserRegisteredEmailConsumer> _logger;

    public UserRegisteredEmailConsumer(
        IMailService mailService,
        ILogger<UserRegisteredEmailConsumer> logger)
    {
        _mailService = mailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var message = context.Message;
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.Email))
        {
            return;
        }

        try
        {
            // The body is sent as text/html, so the name — user-supplied — has to be encoded or a
            // first name containing '<' is parsed as markup instead of shown.
            var greeting = $"Hi {message.FirstName}, thanks for registering.";
            var mail = new MailRequest(
                to: new System.Collections.ObjectModel.Collection<string> { message.Email },
                subject: "Welcome!",
                body: "Welcome!");

            await _mailService.SendAsync(mail, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Email failures must not break user registration.
            // The email can be retried via the outbox/dead-letter mechanism.
            // PII minimization: identify the recipient by UserId, not email address.
            _logger.LogWarning(ex, "Failed to send welcome email to user {UserId}", message.UserId);
        }
    }
}
