namespace Caching.V1;

public class CachingOptions
{
    public string Redis { get; set; } = string.Empty;

    public bool? EnableSsl { get; set; }

    public TimeSpan? DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan? DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(15);

    public string? KeyPrefix { get; set; } = "app_";
}