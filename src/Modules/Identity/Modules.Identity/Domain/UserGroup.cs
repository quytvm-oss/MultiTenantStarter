namespace Modules.Identity.Domain;

public class UserGroup
{
    public string UserId { get; private set; } = default!;

    public Guid GroupId { get; private set; }

    public DateTime AddedAt { get; private set; }

    public string? AddedBy { get; private set; }


    public virtual User? User { get; init; }

    public virtual Group? Group { get; init; }
    
    private UserGroup() { } // EF Core


    public static UserGroup Create(string userId, Guid groupId, string? addedBy = null)
    {
        return new UserGroup()
        {
            UserId = userId,
            GroupId = groupId,
            AddedBy = addedBy,
            AddedAt = TimeProvider.System.GetUtcNow().UtcDateTime
        };
    }
    
}