namespace Shared.Identity;

public record Permission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    public string Name => NameFor(Action, Resource);

    private static string NameFor(string action, string resource)
    {
        return $"Permission.{resource}.{action}";
    }
};