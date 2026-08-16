using Core.Domain;

namespace Modules.Notifications.Domain;

public class NotificationTemplate : AggregateRoot<Guid>
{
    public NotificationType NotificationType { get; set; }

    public string Title { get; set; }

    public string Body { get; set; }

    public string Subject  { get; set; }
    
    public Platform Platform { get; set; }
}

public enum NotificationType
{
}