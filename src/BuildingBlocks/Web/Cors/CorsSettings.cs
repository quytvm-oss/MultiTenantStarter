namespace Web.Cors;

public class CorsSettings
{
    public bool AllowAll { get; init; } = true;
    public string[] AllowedOrigins { get; init; } = [];
    public string[] AllowedHeaders { get; init; } = ["*"];
    public string[] AllowedMethods { get; init; } = ["*"];
}