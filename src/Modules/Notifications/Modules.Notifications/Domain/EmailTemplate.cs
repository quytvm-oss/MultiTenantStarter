using Core.Domain;

namespace Modules.Notifications.Domain;

public class EmailTemplate : AggregateRoot<Guid>
{
    public string? Title { get; set; }
    
    public EmailTemplateType Type { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }
    
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }
}

public enum EmailTemplateType
{
    
}