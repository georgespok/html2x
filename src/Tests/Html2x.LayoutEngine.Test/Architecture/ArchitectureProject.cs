using System.Xml.Linq;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;

internal sealed class ArchitectureProject
{
    private readonly XDocument _document;
    private readonly string _path;

    private ArchitectureProject(string path)
    {
        _path = path;
        _document = XDocument.Load(path);
    }

    public static ArchitectureProject Load(params string[] pathSegments) =>
        new(ArchitecturePaths.PathFromRoot(pathSegments));

    public IReadOnlyList<string> ProjectReferences() =>
        _document.Descendants("ProjectReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => Path.GetFileNameWithoutExtension(value!))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<string> PackageReferences() =>
        _document.Descendants("PackageReference")
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
            actual.ShouldNotContain(forbiddenProject, $"{_path} should not reference {forbiddenProject}.");
        }
    }

    public void ShouldNotReferencePackages(params string[] forbiddenPackages)
    {
        var actual = PackageReferences();
        foreach (var forbiddenPackage in forbiddenPackages)
        {
            actual.ShouldNotContain(forbiddenPackage, $"{_path} should not reference {forbiddenPackage}.");
        }
    }

    public IReadOnlyList<string> TargetFrameworks() =>
        _document.Descendants("TargetFramework")
            .Concat(_document.Descendants("TargetFrameworks"))
            .Select(static element => element.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(static value =>
                value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    public void ShouldHaveNoProjectReferences() =>
        ProjectReferences().ShouldBeEmpty($"{_path} should not reference other projects.");

    public void ShouldHaveNoPackageReferences() =>
        PackageReferences().ShouldBeEmpty($"{_path} should not reference packages.");
}