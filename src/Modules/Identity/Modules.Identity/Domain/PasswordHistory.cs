namespace Modules.Identity.Domain;

public class PasswordHistory
{
    public int Id { get; init; }

    public string UserId { get; private set; } = default!;

    public string PasswordHash { get; private set; } = default!;

    public DateTime CreatedAt { get; set; }
    
    // Navigation property (init for EF Core materialization)
    public virtual User User { get; init; }
    
    private PasswordHistory() {}

    public static PasswordHistory Create(string userId, string passwordHash)
    {
        return new PasswordHistory()
        {
            UserId = userId, PasswordHash = passwordHash, CreatedAt = TimeProvider.System.GetUtcNow().UtcDateTime
        };
    }
}