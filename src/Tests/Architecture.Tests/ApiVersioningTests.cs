using System.Reflection;
using System.Text.RegularExpressions;

using Modules.Auditing;
using Modules.Identity;
using Modules.Multitenancy;

using NetArchTest.Rules;

using Shouldly;

namespace Architecture.Tests;

public partial class ApiVersioningTests
{
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(AuditingModule).Assembly,
        typeof(IdentityModule).Assembly,
        typeof(MultitenancyModule).Assembly
    ];
    
    private static readonly string SolutionRoot = ModuleArchitectureTestsFixture.SolutionRoot;
    
    [Fact]
    public void Features_Should_Be_In_Versioned_Namespace()
    {
        foreach (var module in ModuleAssemblies)
        {
            var result = Types
                .InAssembly(module)
                .That()
                .ResideInNamespaceContaining(".Features.")
                .Should()
                .ResideInNamespaceMatching(@"\.Features\.v\d+")
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"Features in module '{module.GetName().Name}' should be in versioned namespaces (v1, v2, etc.). " +
                $"Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    [Fact]
    public void Feature_Folders_Should_Follow_Version_Convention()
    {
        string moduleRoot  = Path.Combine(SolutionRoot, "src", "Modules");

        if (!Directory.Exists(moduleRoot))
        {
            return;
        }

        var featureFolders = Directory
            .GetDirectories(moduleRoot, "Features", SearchOption.AllDirectories)
            .ToArray();
        
        var violations = new List<string>();

        foreach (var featureFolder in featureFolders)
        {
            var subFolders = Directory.GetDirectories(featureFolder);

            foreach (var subFolder in subFolders)
            {
                string folderName = Path.GetFileName(subFolder);
                
                // Feature folders directly under Features should be version folders (v1, v2, etc.)
                if (!VersionFolderRegex().IsMatch(folderName))
                {
                    violations.Add(
                        $"Folder '{subFolder}' should be a version folder (v1, v2, etc.), not '{folderName}'.");
                }
            }
        }
        
        violations.ShouldBeEmpty($"Feature folders should be organized by version. " +
                                 $"Violations: {string.Join("; ", violations)}");
    }

    [Fact]
    public void V1_Types_Should_Not_Depend_On_Higher_Versions()
    {
        var forbiddenNamespaces = ModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => GetVersionNamespacePrefix(type.Namespace))
            .Where(x => x is not null)
            .Where(x => GetVersionNumber(x!) > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;

        foreach (var assembly in ModuleAssemblies)
        {
            var v1Types = Types
                .InAssembly(assembly)
                .That()
                .ResideInNamespaceContaining(".Features.v1")
                .GetTypes()
                .ToArray();

            if (v1Types.Length == 0)
            {
                continue;
            }

            var result = Types
                .InAssembly(assembly)
                .That()
                .ResideInNamespaceContaining(".Features.v1")
                .ShouldNot()
                .HaveDependencyOnAny(forbiddenNamespaces)
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? [];

            result.IsSuccessful.ShouldBeTrue(
                $"v1 types in assembly '{assembly.GetName().Name}' must not depend " +
                $"on higher API versions. Failing types: {string.Join(", ", failingTypes)}");
        }
    }

    private static string? GetVersionNamespacePrefix(string? namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return null;
        }

        var match = Regex.Match(
            namespaceName,
            @"^(.*\.Features\.v\d+)(?:\.|$)",
            RegexOptions.IgnoreCase);

        return match.Success
            ? match.Groups[1].Value
            : null;
    }

    private static int GetVersionNumber(string versionNamespace)
    {
        var match = Regex.Match(
            versionNamespace,
            @"\.v(\d+)$",
            RegexOptions.IgnoreCase);

        return match.Success
            ? int.Parse(match.Groups[1].Value)
            : 0;
    }
    
    [GeneratedRegex(@"^v\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionFolderRegex();
}