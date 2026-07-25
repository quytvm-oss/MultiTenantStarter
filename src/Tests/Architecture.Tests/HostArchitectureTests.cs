namespace Architecture.Tests;

public class HostArchitectureTests
{
    
}

internal static class ModuleArchitectureTestsFixture
{
    public static readonly string SolutionRoot = GetSolutionRoot();
    
    private static string GetSolutionRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }
        
        if (directory is null)
        {
            throw new InvalidOperationException("Unable to locate solution root containing 'src' folder.");
        }
        
        return directory.FullName;
        
    }
}