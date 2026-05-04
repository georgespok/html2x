using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;


internal sealed class ArchitectureProject
{
    private readonly string path;
    private readonly XDocument document;

    private ArchitectureProject(string path)
    {
        this.path = path;
        document = XDocument.Load(path);
    }

    public static ArchitectureProject Load(params string[] pathSegments) =>
        new(ArchitecturePaths.PathFromRoot(pathSegments));

    public IReadOnlyList<string> ProjectReferences() =>
        document.Descendants("ProjectReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => Path.GetFileNameWithoutExtension(value!))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<string> PackageReferences() =>
        document.Descendants("PackageReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    public void ShouldReferenceProjects(params string[] expectedProjects) =>
        ProjectReferences().ShouldBeSet(expectedProjects);

    public void ShouldReferencePackages(params string[] expectedPackages) =>
        PackageReferences().ShouldBeSet(expectedPackages);

    public void ShouldNotReferenceProjects(params string[] forbiddenProjects)
    {
        var actual = ProjectReferences();
        foreach (var forbiddenProject in forbiddenProjects)
        {
            actual.ShouldNotContain(forbiddenProject, $"{path} should not reference {forbiddenProject}.");
        }
    }

    public void ShouldNotReferencePackages(params string[] forbiddenPackages)
    {
        var actual = PackageReferences();
        foreach (var forbiddenPackage in forbiddenPackages)
        {
            actual.ShouldNotContain(forbiddenPackage, $"{path} should not reference {forbiddenPackage}.");
        }
    }

    public IReadOnlyList<string> TargetFrameworks() =>
        document.Descendants("TargetFramework")
            .Concat(document.Descendants("TargetFrameworks"))
            .Select(static element => element.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(static value => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    public void ShouldHaveNoProjectReferences() =>
        ProjectReferences().ShouldBeEmpty($"{path} should not reference other projects.");

    public void ShouldHaveNoPackageReferences() =>
        PackageReferences().ShouldBeEmpty($"{path} should not reference packages.");
}
