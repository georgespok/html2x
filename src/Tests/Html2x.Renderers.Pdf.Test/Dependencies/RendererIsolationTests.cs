using System.Xml.Linq;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test.Dependencies;

public sealed class RendererIsolationTests
{
    private static readonly string[] ForbiddenReferences =
    [
        "Html2x.Abstractions",
        "Html2x.LayoutEngine",
        "Html2x.LayoutEngine.Style",
        "Html2x.LayoutEngine.Models"
    ];

    [Fact]
    public void RendererProject_ProjectReferences_ExcludeLayoutEngine()
    {
        var projectFile = FindRepositoryFile(
            "src",
            "Html2x.Renderers.Pdf",
            "Html2x.Renderers.Pdf.csproj");

        var references = ReadProjectReferences(projectFile)
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        references.ShouldContain("Html2x.Diagnostics.Contracts");
        references.ShouldContain("Html2x.RenderModel");
        references.ShouldContain("Html2x.Text");
        references.ShouldNotContain("Html2x.Abstractions");
        references.ShouldNotContain("Html2x.LayoutEngine");
    }

    [Fact]
    public void RendererSource_ImportsAreDeclared_DoesNotImportLayoutEngineNamespaces()
    {
        var projectDirectory = Path.GetDirectoryName(FindRepositoryFile(
            "src",
            "Html2x.Renderers.Pdf",
            "Html2x.Renderers.Pdf.csproj"))!;
        var sourceFiles = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                                  !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        var violations = sourceFiles
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new SourceLine(file, index + 1, line)))
            .Where(line => ForbiddenReferences.Any(reference =>
                line.Text.Contains(reference, StringComparison.Ordinal)))
            .Select(line => $"{Path.GetFileName(line.File)}:{line.LineNumber}: {line.Text.Trim()}")
            .ToArray();

        violations.ShouldBeEmpty();
    }

    private static IReadOnlyList<string> ReadProjectReferences(string projectFile)
    {
        var document = XDocument.Load(projectFile);

        return document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectFile)!, value!)))
            .ToArray();
    }

    private static string FindRepositoryFile(params string[] pathParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine([current.FullName, .. pathParts]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find repository file '{Path.Combine(pathParts)}' from '{AppContext.BaseDirectory}'.");
    }

    private sealed record SourceLine(string File, int LineNumber, string Text);
}
