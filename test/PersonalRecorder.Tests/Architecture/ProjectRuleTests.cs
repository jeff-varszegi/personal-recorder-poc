namespace PersonalRecorder.Tests.Architecture;

/// <summary>
/// Project-level rules (docs/ARCHITECTURE.md §5.3, ADR-015), read from the .csproj files: which
/// project may reference which, which must stay platform-neutral, and which packages are
/// forbidden where. Namespace-level rules inside the assemblies are in <see cref="NamespaceRuleTests"/>.
/// </summary>
public sealed class ProjectRuleTests
{
    private const string Core = "PersonalRecorder.Core";
    private const string Client = "PersonalRecorder.Client";
    private const string App = "PersonalRecorder.App";
    private const string Harness = "PersonalRecorder.Harness";
    private const string Web = "PersonalRecorder.Web";
    private const string Service = "PersonalRecorder.Service";
    private const string Verifier = "PersonalRecorder.Verifier";

    /// <summary>The allow-list. A project may reference only what is listed here — six edges in all.</summary>
    private static readonly Dictionary<string, string[]> AllowedReferences = new(StringComparer.Ordinal)
    {
        [Core] = [],
        [Client] = [Core],
        [App] = [Client],
        [Harness] = [Client],
        [Web] = [Core, Client],
        [Service] = [Core],
        [Verifier] = [Core],
    };

    /// <summary>
    /// Core ships open source and runs in WebAssembly and NativeAOT; it takes no dependencies
    /// beyond the BCL. The one planned exception is a libsodium binding (ADR-009); add it here
    /// when it arrives, with its ADR reference.
    /// </summary>
    private static readonly string[] PackagesAllowedInCore = [];

    public static TheoryData<string> ProductionProjects()
    {
        TheoryData<string> data = [];
        foreach (string name in AllowedReferences.Keys)
        {
            data.Add(name);
        }

        return data;
    }

    [Fact]
    public void Every_production_project_has_a_declared_dependency_rule()
    {
        IEnumerable<string> undeclared = SolutionLayout.Projects
            .Select(p => p.Name)
            .Where(name => !name.EndsWith("Tests", StringComparison.Ordinal))
            .Where(name => !AllowedReferences.ContainsKey(name));

        Assert.True(
            !undeclared.Any(),
            $"New project(s) added without a dependency rule: {string.Join(", ", undeclared)}. " +
            "Add them to AllowedReferences and to docs/ARCHITECTURE.md §5.3.");
    }

    [Theory]
    [MemberData(nameof(ProductionProjects))]
    public void Project_references_only_what_the_architecture_allows(string projectName)
    {
        ProjectInfo project = SolutionLayout.Project(projectName);
        string[] allowed = AllowedReferences[projectName];

        IEnumerable<string> violations = project.ProjectReferences.Except(allowed, StringComparer.Ordinal);

        Assert.True(
            !violations.Any(),
            $"{projectName} references {string.Join(", ", violations)}, which docs/ARCHITECTURE.md §5.3 does not permit.");
    }

    [Fact]
    public void Core_is_platform_neutral()
    {
        ProjectInfo core = SolutionLayout.Project(Core);

        Assert.Equal(["net10.0"], core.TargetFrameworks);
    }

    [Fact]
    public void Core_takes_no_package_dependencies()
    {
        ProjectInfo core = SolutionLayout.Project(Core);

        IEnumerable<string> unexpected = core.PackageReferences.Except(PackagesAllowedInCore, StringComparer.Ordinal);

        Assert.True(
            !unexpected.Any(),
            $"Core references {string.Join(", ", unexpected)}. Core is open source and runs in WASM and NativeAOT; " +
            "it depends on the BCL alone (ADR-014, ADR-015).");
    }

    [Fact]
    public void Client_keeps_a_portable_flavour_for_the_browser()
    {
        ProjectInfo client = SolutionLayout.Project(Client);

        Assert.Contains("net10.0", client.TargetFrameworks);
    }

    [Fact]
    public void Capture_and_shared_logic_never_depend_on_a_ui_framework()
    {
        foreach (string name in new[] { Core, Client })
        {
            ProjectInfo project = SolutionLayout.Project(name);

            Assert.DoesNotContain(project.PackageReferences, p => p.StartsWith("Microsoft.Maui", StringComparison.Ordinal));
            Assert.DoesNotContain(project.PackageReferences, p => p.StartsWith("Microsoft.AspNetCore.Components", StringComparison.Ordinal));
            Assert.DoesNotContain(project.ProjectReferences, r => r is App or Web);
        }
    }

    [Fact]
    public void Verifier_is_independent_of_the_service_and_the_network()
    {
        ProjectInfo verifier = SolutionLayout.Project(Verifier);

        Assert.DoesNotContain(verifier.ProjectReferences, r => r.Contains(".Service", StringComparison.Ordinal));
        Assert.DoesNotContain(verifier.PackageReferences, p => p.Contains("AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Service_uses_dapper_not_entity_framework_and_no_identity_framework()
    {
        ProjectInfo service = SolutionLayout.Project(Service);

        Assert.DoesNotContain(service.PackageReferences, p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(service.PackageReferences, p => p.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal));
        Assert.Contains("Dapper", service.PackageReferences);
    }

    [Fact]
    public void Every_project_targets_the_ratified_lts_runtime()
    {
        foreach (ProjectInfo project in SolutionLayout.Projects)
        {
            Assert.All(project.TargetFrameworks, tfm =>
                Assert.True(
                    tfm.StartsWith("net10.0", StringComparison.Ordinal),
                    $"{project.Name} targets '{tfm}'. ADR-001 fixes the solution on .NET 10 LTS; " +
                    "moving off it is an ADR-level decision, not a project-level one."));
        }
    }
}
