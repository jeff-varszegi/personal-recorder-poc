namespace PersonalRecorder.ArchitectureTests;

/// <summary>
/// Enforces docs/ARCHITECTURE.md §5.3. These rules are what keep §4.3 true over time:
/// the UI shell stays swappable because nothing below it is allowed to depend upward,
/// and the capture core stays framework-free because the build says so.
/// </summary>
public sealed class DependencyRuleTests
{
    private const string Contracts = "PersonalRecorder.Contracts";
    private const string Evidence = "PersonalRecorder.Evidence";
    private const string CaptureAbstractions = "PersonalRecorder.Client.Capture.Abstractions";
    private const string CaptureAndroid = "PersonalRecorder.Client.Capture.Android";
    private const string CaptureIos = "PersonalRecorder.Client.Capture.Ios";
    private const string ClientCore = "PersonalRecorder.Client.Core";
    private const string Domain = "PersonalRecorder.Service.Domain";
    private const string Application = "PersonalRecorder.Service.Application";
    private const string Infrastructure = "PersonalRecorder.Service.Infrastructure";
    private const string Workers = "PersonalRecorder.Service.Workers";

    /// <summary>The allow-list. A project may reference only what is listed here.</summary>
    private static readonly Dictionary<string, string[]> AllowedReferences = new(StringComparer.Ordinal)
    {
        [Contracts] = [],
        [Evidence] = [Contracts],
        [CaptureAbstractions] = [Evidence, Contracts],
        [CaptureAndroid] = [CaptureAbstractions],
        [CaptureIos] = [CaptureAbstractions],
        [ClientCore] = [CaptureAbstractions, Evidence, Contracts],
        ["PersonalRecorder.Client.App"] = [ClientCore, CaptureAbstractions, CaptureAndroid, CaptureIos],
        ["PersonalRecorder.Client.AndroidHarness"] = [CaptureAbstractions, CaptureAndroid],
        ["PersonalRecorder.Web"] = [Contracts, Evidence, ClientCore],
        ["PersonalRecorder.Verifier"] = [Evidence, Contracts],
        [Domain] = [],
        [Application] = [Domain, Contracts],
        [Infrastructure] = [Application],
        [Workers] = [Application],
        ["PersonalRecorder.Service.Api"] = [Application, Infrastructure, Workers, Contracts],
    };

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

    [Theory]
    [InlineData(Contracts)]
    [InlineData(Evidence)]
    [InlineData(CaptureAbstractions)]
    [InlineData(ClientCore)]
    [InlineData(Domain)]
    public void Framework_free_projects_target_no_platform(string projectName)
    {
        ProjectInfo project = SolutionLayout.Project(projectName);

        Assert.All(project.TargetFrameworks, tfm =>
            Assert.True(
                tfm == "net10.0",
                $"{projectName} targets '{tfm}'. It must stay platform-neutral (net10.0) so the capture core " +
                "and shared logic remain portable across shells, hosts, and the browser."));
    }

    [Fact]
    public void Capture_cores_never_depend_on_a_ui_framework()
    {
        foreach (string captureProject in new[] { CaptureAndroid, CaptureIos, CaptureAbstractions })
        {
            ProjectInfo project = SolutionLayout.Project(captureProject);

            Assert.DoesNotContain(project.PackageReferences, p => p.StartsWith("Microsoft.Maui", StringComparison.Ordinal));
            Assert.DoesNotContain(project.ProjectReferences, r => r.EndsWith(".App", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Verifier_is_independent_of_the_service_and_the_network()
    {
        ProjectInfo verifier = SolutionLayout.Project("PersonalRecorder.Verifier");

        Assert.DoesNotContain(verifier.ProjectReferences, r => r.Contains(".Service.", StringComparison.Ordinal));
        Assert.DoesNotContain(verifier.PackageReferences, p => p.Contains("AspNetCore", StringComparison.Ordinal));
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
