using System.Reflection;
using NetArchTest.Rules;
using PersonalRecorder.Client.Capture;
using PersonalRecorder.Evidence;

namespace PersonalRecorder.Tests.Architecture;

/// <summary>
/// Namespace-level dependency rules (docs/ARCHITECTURE.md §5.3, ADR-015). With one assembly per
/// deployable, project references can no longer say "capture must not depend on presentation";
/// these rules read the compiled IL and fail when a type in one namespace reaches into a
/// namespace it must not. Several are vacuous until the namespaces gain types — that is the
/// point: the rule exists before the first violation can.
/// </summary>
public sealed class NamespaceRuleTests
{
    private static readonly Assembly Core = typeof(HashChain).Assembly;
    private static readonly Assembly Client = typeof(IRecordingEngine).Assembly;
    private static readonly Assembly Service = Assembly.Load("PersonalRecorder.Service");
    private static readonly Assembly Verifier = Assembly.Load("PersonalRecorder.Verifier");

    private static readonly string[] UiFrameworks =
    [
        "Microsoft.Maui",
        "Microsoft.AspNetCore.Components",
        "Microsoft.UI",
        "Android.",
        "UIKit",
    ];

    /// <summary>Vertical features. They meet only at Data and at the entry points, never directly.</summary>
    private static readonly string[] ServiceFeatures =
    [
        "PersonalRecorder.Service.Ingest",
        "PersonalRecorder.Service.Artifacts",
        "PersonalRecorder.Service.Circles",
        "PersonalRecorder.Service.Alerts",
        "PersonalRecorder.Service.Heartbeat",
        "PersonalRecorder.Service.Sharing",
        "PersonalRecorder.Service.Identity",
    ];

    private static readonly string[] ServiceEntryPoints =
    [
        "PersonalRecorder.Service.Controllers",
        "PersonalRecorder.Service.Hubs",
        "PersonalRecorder.Service.Workers",
    ];

    private static readonly string[] ServiceInfrastructure =
    [
        "PersonalRecorder.Service.Data",
        "PersonalRecorder.Service.Storage",
        "PersonalRecorder.Service.Messaging",
    ];

    public static TheoryData<string> FeatureNamespaces()
    {
        TheoryData<string> data = [];
        foreach (string feature in ServiceFeatures)
        {
            data.Add(feature);
        }

        return data;
    }

    [Fact]
    public void The_assemblies_under_test_are_the_real_ones()
    {
        Assert.Equal("PersonalRecorder.Core", Core.GetName().Name);
        Assert.Equal("PersonalRecorder.Client", Client.GetName().Name);
        Assert.Equal("PersonalRecorder.Service", Service.GetName().Name);
        Assert.Equal("PersonalRecorder.Verifier", Verifier.GetName().Name);
    }

    /// <summary>
    /// The rules below select by namespace prefix, so that a feature's sub-namespaces are
    /// covered. This pins that <c>ResideInNamespace</c> really is a prefix match.
    /// </summary>
    [Fact]
    public void Namespace_selection_includes_sub_namespaces()
    {
        TestResult result = Types.InAssembly(Client)
            .That().ResideInNamespace("PersonalRecorder.Client")
            .Should().HaveNameStartingWith("NoTypeIsCalledThis")
            .GetResult();

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.FailingTypes, type => type.FullName == typeof(IRecordingEngine).FullName);
    }

    [Fact]
    public void Core_references_the_bcl_alone()
    {
        AssertReferencesOnly(Core, name => name.StartsWith("System.", StringComparison.Ordinal) && !name.StartsWith("System.Net", StringComparison.Ordinal));
    }

    [Fact]
    public void Core_never_reaches_up_into_a_host()
    {
        AssertRule(Types.InAssembly(Core)
            .ShouldNot()
            .HaveDependencyOnAny("PersonalRecorder.Client", "PersonalRecorder.Service", "PersonalRecorder.Web", "PersonalRecorder.App")
            .GetResult());
    }

    [Fact]
    public void Verifier_references_neither_the_service_nor_a_network_stack()
    {
        AssertReferencesOnly(
            Verifier,
            name => name.StartsWith("System.", StringComparison.Ordinal) && !name.StartsWith("System.Net", StringComparison.Ordinal)
                || name == "PersonalRecorder.Core");
    }

    [Fact]
    public void Capture_core_never_depends_on_a_ui_framework_or_on_presentation()
    {
        AssertRule(Types.InAssembly(Client)
            .That().ResideInNamespace("PersonalRecorder.Client.Capture")
            .ShouldNot().HaveDependencyOnAny([.. UiFrameworks, "PersonalRecorder.Client.Presentation"])
            .GetResult());
    }

    [Fact]
    public void Presentation_never_depends_on_a_ui_framework()
    {
        AssertRule(Types.InAssembly(Client)
            .That().ResideInNamespace("PersonalRecorder.Client.Presentation")
            .ShouldNot().HaveDependencyOnAny(UiFrameworks)
            .GetResult());
    }

    [Theory]
    [MemberData(nameof(FeatureNamespaces))]
    public void Service_features_do_not_depend_on_each_other_or_on_entry_points(string feature)
    {
        string[] forbidden = [.. ServiceFeatures.Where(f => f != feature), .. ServiceEntryPoints];

        AssertRule(Types.InAssembly(Service)
            .That().ResideInNamespace(feature)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult());
    }

    [Fact]
    public void Service_infrastructure_does_not_depend_on_features_or_entry_points()
    {
        foreach (string infrastructure in ServiceInfrastructure)
        {
            AssertRule(Types.InAssembly(Service)
                .That().ResideInNamespace(infrastructure)
                .ShouldNot().HaveDependencyOnAny([.. ServiceFeatures, .. ServiceEntryPoints])
                .GetResult());
        }
    }

    private static void AssertReferencesOnly(Assembly assembly, Func<string, bool> permitted)
    {
        IEnumerable<string> offending = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !permitted(name));

        Assert.True(
            !offending.Any(),
            $"{assembly.GetName().Name} references {string.Join(", ", offending)}, which docs/ARCHITECTURE.md §5.3 does not permit.");
    }

    private static void AssertRule(TestResult result)
    {
        IEnumerable<string> failing = (result.FailingTypes ?? []).Select(type => type.FullName);

        Assert.True(result.IsSuccessful, $"Violations: {string.Join(", ", failing)}");
    }
}
