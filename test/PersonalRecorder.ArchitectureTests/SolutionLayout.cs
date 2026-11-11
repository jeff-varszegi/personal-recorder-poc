using System.Xml.Linq;

namespace PersonalRecorder.ArchitectureTests;

/// <summary>
/// Reads the solution's project graph straight from the .csproj files, so the dependency
/// rules in docs/ARCHITECTURE.md §5.3 are enforced from the first commit — before any
/// assembly exists to inspect.
/// </summary>
internal sealed record ProjectInfo(
    string Name,
    string Path,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences);

internal static class SolutionLayout
{
    private static readonly Lazy<IReadOnlyList<ProjectInfo>> LazyProjects = new(Load);

    public static IReadOnlyList<ProjectInfo> Projects => LazyProjects.Value;

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static ProjectInfo Project(string name) =>
        Projects.SingleOrDefault(p => p.Name == name)
        ?? throw new InvalidOperationException($"Project '{name}' was not found in the solution.");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PersonalRecorder.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root (PersonalRecorder.slnx).");
    }

    private static IReadOnlyList<ProjectInfo> Load()
    {
        string[] roots = [Path.Combine(RepositoryRoot, "components"), Path.Combine(RepositoryRoot, "test")];

        return roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
            .Select(Parse)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static ProjectInfo Parse(string path)
    {
        XDocument doc = XDocument.Load(path);

        List<string> frameworks = doc.Descendants()
            .Where(e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .SelectMany(e => e.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(tfm => !tfm.StartsWith("$(", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        List<string> projectRefs = doc.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', Path.DirectorySeparatorChar)))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        List<string> packageRefs = doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new ProjectInfo(Path.GetFileNameWithoutExtension(path), path, frameworks, projectRefs, packageRefs);
    }
}
