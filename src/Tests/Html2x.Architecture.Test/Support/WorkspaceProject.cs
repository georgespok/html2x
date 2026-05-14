using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Shouldly;

namespace Html2x.Architecture.Test.Support;

internal sealed class WorkspaceProject
{
    private readonly string _path;

    private WorkspaceProject(
        string path,
        string assemblyName,
        IReadOnlyList<string> projectReferences,
        IReadOnlyList<string> sourceFilePaths)
    {
        _path = path;
        AssemblyName = assemblyName;
        ProjectReferences = projectReferences;
        SourceFilePaths = sourceFilePaths;
    }

    public string AssemblyName { get; }

    public IReadOnlyList<string> ProjectReferences { get; }

    public IReadOnlyList<string> SourceFilePaths { get; }

    public static WorkspaceProject Load(params string[] pathSegments)
    {
        var path = Paths.PathFromRoot(pathSegments);
        using var workspace = MSBuildWorkspace.Create();
        var project = workspace.OpenProjectAsync(path).GetAwaiter().GetResult();
        var failures = workspace.Diagnostics
            .Where(static diagnostic => diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            .Select(static diagnostic => diagnostic.Message)
            .ToArray();
        failures.ShouldBeEmpty($"MSBuild workspace should load {path} without failures.");

        var references = project.ProjectReferences
            .Select(reference => project.Solution.GetProject(reference.ProjectId)?.AssemblyName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name!)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var sourceFiles = project.Documents
            .Select(static document => document.FilePath)
            .Where(static file => !string.IsNullOrWhiteSpace(file))
            .Select(static file => file!)
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();

        return new(
            path,
            project.AssemblyName,
            references,
            sourceFiles);
    }

    public void ShouldHaveAssemblyName(string expectedAssemblyName) =>
        AssemblyName.ShouldBe(expectedAssemblyName, $"{_path} assembly name mismatch.");

    public void ShouldReferenceProjects(params string[] expectedProjects) =>
        ProjectReferences.ShouldBeSet(expectedProjects);
}
