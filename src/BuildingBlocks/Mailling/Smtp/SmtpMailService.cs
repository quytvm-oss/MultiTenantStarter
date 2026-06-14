using MailKit.Net.Smtp;
using MailKit.Security;

using Mailling.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

namespace Mailling.Smtp;

public class SmtpMailService : IMailService
{
    private readonly MailOptions _settings;
    private readonly ILogger<SmtpMailService> _logger;

    public SmtpMailService(IOptions<MailOptions> settings, ILogger<SmtpMailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_settings.Smtp?.Host is null)
        {
            throw new InvalidOperationException("SMTP host is not configured");
        }
        
        var email = new MimeMessage();
        ConfigureSender(email, request);
        AddMetadata(email, request);
        await AddAttachmentsAsync(email, request, ct);
        await SendMailAsync(email, ct);
    }

    private void ConfigureSender(MimeMessage email, MailRequest request)
    {
        var fromAddress =
            request.From ?? _settings.From ?? throw new InvalidOperationException("From is not configured");
        email.From.Add(new MailboxAddress(_settings.DisplayName, fromAddress));
        email.Sender = new MailboxAddress(request.DisplayName ?? _settings.DisplayName, fromAddress);
        
        foreach (string address in request.To.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            email.To.Add(MailboxAddress.Parse(address.Trim()));
        }

        if (!string.IsNullOrEmpty(request.ReplyTo))
        {
            email.ReplyTo.Add(new MailboxAddress(request.ReplyToName, request.ReplyTo));
        }

        if (request?.Bcc is not null && request.Bcc.Count != 0)
        {
            foreach (string address in request.Bcc.Where(bcc => !string.IsNullOrWhiteSpace(bcc)))
            {
                email.Bcc.Add(MailboxAddress.Parse(address.Trim()));
            }
        }

        if (request?.Cc is not null && request.Cc.Count != 0)
        {
            foreach (var address in request.Cc.Where(cc => !string.IsNullOrWhiteSpace(cc)))
            {
                email.Cc.Add(MailboxAddress.Parse(address.Trim()));
            }
        }
    }
    
    private void AddMetadata(MimeMessage email, MailRequest request)
    {
        if (request?.Headers is not null)
        {
            foreach (var header in request.Headers)
            {
                email.Headers.Add(header.Key, header.Value);
            }
        }

        if (request?.Subject != null)
        {
            email.Subject = request.Subject;
        }
    }

    private async Task AddAttachmentsAsync(MimeMessage email, MailRequest request, CancellationToken ct)
    {
        var builder = new BodyBuilder()
        {
            HtmlBody = request.Body,
        };

        if (request?.AttachmentData is not null && request.AttachmentData.Count != 0)
        {
            foreach (var attachment in request.AttachmentData)
            {
                using var stream = new MemoryStream();
                await stream.WriteAsync(attachment.Value, ct);
                stream.Position = 0;
                await builder.Attachments.AddAsync(attachment.Key, stream, ct);
            }
        }
        
        email.Body = builder.ToMessageBody();
    }

    private async Task SendMailAsync(MimeMessage email, CancellationToken ct)
    {
        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_settings.Smtp!.Host!, _settings.Smtp.Port, SecureSocketOptions.StartTls, ct);

            if (!string.IsNullOrWhiteSpace(_settings.Smtp.UserName) &&
                !string.IsNullOrWhiteSpace(_settings.Smtp.Password))
            {
                await client.AuthenticateAsync(_settings.Smtp.UserName, _settings.Smtp.Password, ct);
            }

            await client.SendAsync(email, ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while sending email: {Message}", e.Message);
            throw new InvalidOperationException("Failed to send email.", e);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, ct);
            }
        }
    }
}