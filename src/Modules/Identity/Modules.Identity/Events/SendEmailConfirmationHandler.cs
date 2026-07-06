using System.Collections.ObjectModel;
using System.Text;

using Core.Common;
using Core.Exceptions;

using Jobs.Services;

using Mailling;
using Mailling.Abstractions;

using Mediator;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

using Modules.Identity.Domain;
using Modules.Identity.Domain.Events;

using Shared.Multitenancy;

namespace Modules.Identity.Events;

public sealed class SendEmailConfirmationHandler(
    UserManager<User> userManager, 
    IMailService mailService,
    IJobService jobService,
    ILogger<SendEmailConfirmationHandler> logger)
    : INotificationHandler<EmailConfirmationRequestedEvent>
{
    public async ValueTask Handle(EmailConfirmationRequestedEvent notification, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(notification.UserId)
                   ?? throw new NotFoundException($"User {notification.UserId} was not found.");
        
        if (string.IsNullOrEmpty(user.Email))
        {
            return;
        }
        
        string emailVerificationUri = await GetEmailVerificationUriAsync(user, notification.Origin, notification.TenantId!);
        string emailBody = BuildConfirmationEmailHtml(user.FirstName ?? user.UserName ?? "User", emailVerificationUri);

        var mailRequest = new MailRequest(
            new Collection<string> { user.Email },
            "Confirm Your Email Address",
            emailBody);

        jobService.Enqueue("email", () => mailService.SendAsync(mailRequest, CancellationToken.None));
    }
    
    private async Task<string> GetEmailVerificationUriAsync(User user, string origin, string tenantId)
    {

        string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        const string route = "api/v1/identity/confirm-email";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));

        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), QueryStringKeys.UserId, user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, QueryStringKeys.Code, code);
        verificationUri = QueryHelpers.AddQueryString(
            verificationUri,
            MultitenancyConstants.Identifier,
            tenantId
            );

        return verificationUri;
    }
    
    private static string BuildConfirmationEmailHtml(string userName, string confirmationUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Confirm Your Email</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;">
                <table role="presentation" style="width: 100%; border-collapse: collapse;">
                    <tr>
                        <td align="center" style="padding: 40px 0;">
                            <table role="presentation" style="width: 100%; max-width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                                <tr>
                                    <td style="padding: 40px 40px 30px 40px; text-align: center; background-color: #2563eb; border-radius: 8px 8px 0 0;">
                                        <h1 style="margin: 0; color: #ffffff; font-size: 24px; font-weight: 600;">Confirm Your Email Address</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 40px;">
                                        <p style="margin: 0 0 20px 0; color: #334155; font-size: 16px; line-height: 1.6;">
                                            Hi {System.Net.WebUtility.HtmlEncode(userName)},
                                        </p>
                                        <p style="margin: 0 0 20px 0; color: #334155; font-size: 16px; line-height: 1.6;">
                                            Thank you for registering! Please confirm your email address by clicking the button below:
                                        </p>
                                        <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                            <tr>
                                                <td align="center" style="padding: 30px 0;">
                                                    <a href="{System.Net.WebUtility.HtmlEncode(confirmationUrl)}" style="display: inline-block; padding: 14px 32px; background-color: #2563eb; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 6px;">
                                                        Confirm Email Address
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>
                                        <p style="margin: 0 0 20px 0; color: #64748b; font-size: 14px; line-height: 1.6;">
                                            If the button doesn't work, copy and paste this link into your browser:
                                        </p>
                                        <p style="margin: 0 0 20px 0; color: #2563eb; font-size: 14px; line-height: 1.6; word-break: break-all;">
                                            {System.Net.WebUtility.HtmlEncode(confirmationUrl)}
                                        </p>
                                        <p style="margin: 30px 0 0 0; color: #64748b; font-size: 14px; line-height: 1.6;">
                                            If you didn't create an account, you can safely ignore this email.
                                        </p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 20px 40px; background-color: #f1f5f9; border-radius: 0 0 8px 8px; text-align: center;">
                                        <p style="margin: 0; color: #94a3b8; font-size: 12px;">
                                            This is an automated message. Please do not reply to this email.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}