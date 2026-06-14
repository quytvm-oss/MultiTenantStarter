namespace Mailling.Abstractions;

public interface IMailService
{
    Task SendAsync(MailRequest request, CancellationToken ct);
}