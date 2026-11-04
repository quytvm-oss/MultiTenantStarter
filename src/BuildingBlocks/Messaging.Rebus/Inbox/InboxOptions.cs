namespace Messaging.Rebus.Inbox;

public sealed class InboxOptions
{
    public bool Enabled { get; set; } = true;
    public string SchemaName { get; set; } = "messaging";
    public string TableName { get; set; } = "inbox_messages";
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan RenewalInterval { get; set; } = TimeSpan.FromSeconds(45);
    public TimeSpan BusyRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    public int CleanupBatchSize { get; set; } = 1000;

    internal void Validate()
    {
        if (LeaseDuration <= TimeSpan.Zero || RenewalInterval <= TimeSpan.Zero || RenewalInterval >= LeaseDuration)
            throw new ArgumentException("Inbox renewal interval must be positive and shorter than the lease duration.");
        if (Retention <= TimeSpan.Zero || CleanupInterval <= TimeSpan.Zero || CleanupBatchSize <= 0 || BusyRetryDelay <= TimeSpan.Zero)
            throw new ArgumentException("Inbox retention, cleanup interval and batch size must be positive.");
        ValidateIdentifier(SchemaName);
        ValidateIdentifier(TableName);
    }

    private static void ValidateIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 63 ||
            !(char.IsAsciiLetter(name[0]) || name[0] == '_') ||
            name.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '_')))
            throw new ArgumentException("Inbox schema/table names must be simple PostgreSQL identifiers of at most 63 characters.");
    }
}
