using Core.Domain;

namespace Modules.Notifications.Domain;

public class EmailTemplate : AggregateRoot<Guid>
{
    public string? Title { get; set; }
    
    public EmailTemplateType Type { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }
}

public enum EmailTemplateType
{
    
}