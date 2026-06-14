using System.ComponentModel.DataAnnotations;

namespace Mailling;

public class MailOptions
{
    [Required]
    public string Provider { get; set; } = "Smtp";
    
    public string? From { get; set; }
    
    public string? DisplayName { get; set; }
    
    public SmtpOptions? Smtp { get; set; }
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
