using System.Xml.Linq;
using Shouldly;

namespace Html2x.Architecture.Test.Support;

internal sealed class Project
{
    private readonly XDocument _document;
    private readonly string _path;

    private Project(string path)
    {
        _path = path;
        _document = XDocument.Load(path);
    }

    public static Project Load(params string[] pathSegments) =>
        new(Paths.PathFromRoot(pathSegments));

    public IReadOnlyList<string> ProjectReferences() =>
        _document.Descendants(ProjectFileVocabulary.ProjectReferenceElement)
            .Select(static element => element.Attribute(ProjectFileVocabulary.IncludeAttribute)?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => Path.GetFileNameWithoutExtension(value!))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<string> PackageReferences() =>
        _document.Descendants(ProjectFileVocabulary.PackageReferenceElement)
            .Select(static element => element.Attribute(ProjectFileVocabulary.IncludeAttribute)?.Value)
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
        _document.Descendants(ProjectFileVocabulary.TargetFrameworkElement)
            .Concat(_document.Descendants(ProjectFileVocabulary.TargetFrameworksElement))
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
