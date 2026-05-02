using System.Text.RegularExpressions;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class DiagnosticsBoundaryArchitectureTests
{
    private static readonly Regex DiagnosticsEventsMutationRegex = new(
        @"\.Events\.(?:Add|AddRange|Clear|Remove|RemoveAt|Insert)\b",
        RegexOptions.Compiled);

    [Fact]
    public void DiagnosticsBoundaryDocs_DefineProductionRulesAndOwnership()
    {
        var source = ReadSource("docs", "architecture", "diagnostics-boundary.md");

        source.ShouldContain("Html2x.Diagnostics.Contracts");
        source.ShouldContain("`DiagnosticFields` must not accept arbitrary `object`");
        source.ShouldContain("Runtime Flow");
        source.ShouldContain("Runtime Ownership");
        source.ShouldContain("Facade Boundary");
        source.ShouldContain("Emission Rule");
        source.ShouldContain("Renderer diagnostics flow through the contracts project boundary");
        source.ShouldContain("Html2x.LayoutEngine.Pagination -> Html2x.Diagnostics.Contracts");
        source.ShouldContain("The diagnostics runtime must not reference pagination, layout stages, or producer-local event names");
    }

    [Fact]
    public void DiagnosticsContractsProject_IsInSolution()
    {
        var solution = ReadSource("src", "Html2x.sln");

        solution.ShouldContain("Html2x.Diagnostics.Contracts.csproj");
    }

    [Fact]
    public void DiagnosticsContractsProject_HasNoProjectOrPackageReferences()
    {
        var source = ReadSource(
            "src",
            "Html2x.Diagnostics.Contracts",
            "Html2x.Diagnostics.Contracts.csproj");

        source.ShouldNotContain("<ProjectReference");
        source.ShouldNotContain("<PackageReference");
    }

    [Fact]
    public void DiagnosticsContractsSource_DoesNotAcceptArbitraryObjectsOrStagePayloads()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Diagnostics.Contracts");
        var forbiddenTokens = new[]
        {
            "object?",
            "object ",
            "IDiagnosticsPayload",
            "DiagnosticsSession",
            "DiagnosticsEvent",
            "Payload",
            "Snapshot",
            "TableBox",
            "TableLayoutResult",
            "Html2x.LayoutEngine",
            "Html2x.Renderers",
            "AngleSharp",
            "SkiaSharp"
        };

        foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, file);
            if (IsBuildOutputPath(relativePath))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep diagnostics contracts generic and free of arbitrary object or producer-specific tokens.");
            }
        }
    }

    [Fact]
    public void StageProjects_DoNotReferenceDiagnosticsRuntime()
    {
        var unexpectedReferences = new List<string>();

        foreach (var projectPath in StageProjectPaths())
        {
            var source = ReadSource(projectPath);
            if (source.Contains("Html2x.Diagnostics.csproj", StringComparison.Ordinal))
            {
                unexpectedReferences.Add(projectPath);
            }
        }

        unexpectedReferences.ShouldBeEmpty(
            "Stage projects should not add direct references to Html2x.Diagnostics; use Html2x.Diagnostics.Contracts.");
    }

    [Fact]
    public void StageProjects_MayReferenceDiagnosticsContractsWithoutDiagnosticsRuntime()
    {
        foreach (var projectPath in StageProjectPaths())
        {
            var source = ReadSource(projectPath);
            if (!source.Contains("Html2x.Diagnostics.Contracts.csproj", StringComparison.Ordinal))
            {
                continue;
            }

            source.Contains("Html2x.Diagnostics.csproj", StringComparison.Ordinal).ShouldBeFalse(
                $"{projectPath} should use the diagnostics contracts project without also referencing the diagnostics runtime.");
        }
    }

    [Fact]
    public void PaginationProject_ReferencesDiagnosticsContractsWithoutDiagnosticsRuntime()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Pagination",
            "Html2x.LayoutEngine.Pagination.csproj");

        source.ShouldContain("Html2x.Diagnostics.Contracts.csproj");
        source.ShouldNotContain("Html2x.Diagnostics.csproj");
    }

    [Fact]
    public void PaginationDiagnostics_AreLocalToPaginationProject()
    {
        var source = ReadSource(
            "src",
            "Html2x.LayoutEngine.Pagination",
            "PaginationDiagnostics.cs");
        var oldPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Html2x.LayoutEngine",
            "Diagnostics",
            "PaginationDiagnostics.cs");

        File.Exists(oldPath).ShouldBeFalse();
        source.ShouldContain("namespace Html2x.LayoutEngine.Pagination;");
        source.ShouldContain("internal static class PaginationDiagnostics");
        source.ShouldContain("IDiagnosticsSink?");
        source.ShouldContain("layout/pagination/page-created");
        source.ShouldContain("layout/pagination/block-moved-next-page");
        source.ShouldContain("layout/pagination/oversized-block");
        source.ShouldContain("layout/pagination/empty-document");
    }

    [Fact]
    public void PipelineBoundaries_AcceptDiagnosticsSink()
    {
        var htmlConverter = ReadSource("src", "Html2x", "HtmlConverter.cs");
        var result = ReadSource("src", "Html2x", "Html2PdfResult.cs");
        var layoutBuilder = ReadSource("src", "Html2x.LayoutEngine", "LayoutBuilder.cs");
        var styleBuilder = ReadSource("src", "Html2x.LayoutEngine.Style", "StyleTreeBuilder.cs");
        var styleBuilderInterface = ReadSource("src", "Html2x.LayoutEngine.Style", "IStyleTreeBuilder.cs");
        var geometryBuilder = ReadSource("src", "Html2x.LayoutEngine.Geometry", "Geometry", "LayoutGeometryBuilder.cs");
        var paginator = ReadSource("src", "Html2x.LayoutEngine.Pagination", "LayoutPaginator.cs");
        var renderer = ReadSource("src", "Html2x.Renderers.Pdf", "Pipeline", "PdfRenderer.cs");

        htmlConverter.ShouldContain("new DiagnosticsCollector(");
        htmlConverter.ShouldContain("DiagnosticsReport = report");
        result.ShouldContain("public DiagnosticsReport? DiagnosticsReport");
        layoutBuilder.ShouldContain("IDiagnosticsSink? diagnosticsSink = null");
        styleBuilder.ShouldContain("IDiagnosticsSink? diagnosticsSink = null");
        styleBuilderInterface.ShouldContain("IDiagnosticsSink? diagnosticsSink = null");
        geometryBuilder.ShouldContain("IDiagnosticsSink? diagnosticsSink = null");
        paginator.ShouldContain("IDiagnosticsSink? diagnosticsSink = null");
        renderer.ShouldContain("IDiagnosticsSink? diagnosticsSink = null");
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotReferenceStageParserOrRendererDependencies()
    {
        var projectSource = ReadSource("src", "Html2x.Diagnostics", "Html2x.Diagnostics.csproj");
        var sourceDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.Diagnostics");
        var forbiddenTokens = new[]
        {
            "Html2x.LayoutEngine",
            "Html2x.LayoutEngine.Pagination",
            "Html2x.Renderers.Pdf",
            "AngleSharp",
            "SkiaSharp"
        };

        foreach (var token in forbiddenTokens)
        {
            projectSource.ShouldNotContain(token);
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            if (IsBuildOutputPath(relativePath))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep Html2x.Diagnostics independent from stage, parser, and renderer dependencies.");
            }
        }
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotReferencePaginationOrLayoutStageNames()
    {
        var sourceDirectory = Path.Combine(FindRepoRoot(), "src", "Html2x.Diagnostics");
        var forbiddenTokens = new[]
        {
            "Html2x.LayoutEngine.Pagination",
            "stage/pagination",
            "layout/pagination",
            "layout/geometry-snapshot",
            "style/unsupported-declaration",
            "font/resolve",
            "image/render"
        };

        foreach (var file in Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            if (IsBuildOutputPath(relativePath))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                    $"{relativePath} should keep Html2x.Diagnostics independent from pagination and producer-local stage names.");
            }
        }
    }

    [Fact]
    public void DiagnosticsRuntime_OwnsCollectorReportAndReportSerializer()
    {
        var collector = ReadSource("src", "Html2x.Diagnostics", "DiagnosticsCollector.cs");
        var report = ReadSource("src", "Html2x.Diagnostics", "DiagnosticsReport.cs");
        var serializer = ReadSource("src", "Html2x.Diagnostics", "DiagnosticsReportSerializer.cs");

        collector.ShouldContain("public sealed class DiagnosticsCollector : IDiagnosticsSink");
        collector.ShouldContain("public DiagnosticsReport ToReport(");
        report.ShouldContain("public sealed class DiagnosticsReport");
        report.ShouldContain("public IReadOnlyList<DiagnosticRecord> Records");
        serializer.ShouldContain("public static string ToJson(DiagnosticsReport report)");
    }

    [Fact]
    public void DiagnosticsReportSerializer_ReferencesOnlyContractsAndDiagnosticsReportTypes()
    {
        var source = ReadSource("src", "Html2x.Diagnostics", "DiagnosticsReportSerializer.cs");
        var forbiddenTokens = new[]
        {
            "Html2x.Abstractions",
            "IDiagnosticsPayload",
            "HtmlPayload",
            "LayoutSnapshotPayload",
            "GeometrySnapshotPayload",
            "RenderSummaryPayload",
            "MarginCollapsePayload",
            "TableLayoutPayload",
            "UnsupportedStructurePayload",
            "PaginationTracePayload",
            "StyleDiagnosticPayload",
            "FontResolutionPayload",
            "ImageRenderPayload",
            "LayoutSnapshot",
            "GeometrySnapshot",
            "FragmentSnapshot",
            "TableBox",
            "TableLayoutResult",
            "Html2x.LayoutEngine",
            "Html2x.Renderers",
            "AngleSharp",
            "SkiaSharp"
        };

        source.ShouldContain("using Html2x.Diagnostics.Contracts;");
        source.ShouldContain("DiagnosticsReport");
        source.ShouldContain("DiagnosticValue");
        foreach (var token in forbiddenTokens)
        {
            source.Contains(token, StringComparison.Ordinal).ShouldBeFalse(
                $"DiagnosticsReportSerializer should serialize generic records without `{token}` special cases.");
        }
    }

    [Fact]
    public void DiagnosticsCollections_AreNotMutatedDirectly()
    {
        var mutationSites = FindDiagnosticsEventsMutationSites();

        mutationSites.ShouldBeEmpty(
            "Production diagnostics should be emitted through IDiagnosticsSink.Emit, not direct collection mutation.");
    }

    [Fact]
    public void AbstractionsDiagnosticsTypes_AreRemoved()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions");
        var solution = ReadSource("src", "Html2x.sln");

        Directory.Exists(directory).ShouldBeFalse("the obsolete options-only module should be deleted.");
        solution.ShouldNotContain("Html2x.Abstractions");
    }

    [Fact]
    public void StageProjects_ReferenceContractsAndDoNotReferenceDiagnosticsRuntime()
    {
        foreach (var projectPath in StageProjectPaths())
        {
            var source = ReadSource(projectPath);

            source.ShouldContain("Html2x.Diagnostics.Contracts.csproj");
            source.ShouldNotContain("Html2x.Diagnostics.csproj");
        }
    }

    [Fact]
    public void AbstractionsProject_ContainsNoDiagnosticsTypesAfterMigration()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Html2x.Abstractions");

        Directory.Exists(directory).ShouldBeFalse("diagnostics cannot leak into a deleted obsolete module.");
    }

    private static IReadOnlyList<string> StageProjectPaths() =>
    [
        Path.Combine("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj"),
        Path.Combine("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj"),
        Path.Combine("src", "Html2x.LayoutEngine.Pagination", "Html2x.LayoutEngine.Pagination.csproj"),
        Path.Combine("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj"),
        Path.Combine("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj")
    ];

    private static IReadOnlyList<DiagnosticsEventsMutationSite> FindDiagnosticsEventsMutationSites()
    {
        var root = FindRepoRoot();
        var sourceRoots = new[]
        {
            Path.Combine(root, "src", "Html2x"),
            Path.Combine(root, "src", "Html2x.LayoutEngine"),
            Path.Combine(root, "src", "Html2x.LayoutEngine.Pagination"),
            Path.Combine(root, "src", "Html2x.LayoutEngine.Style"),
            Path.Combine(root, "src", "Html2x.LayoutEngine.Geometry"),
            Path.Combine(root, "src", "Html2x.Renderers.Pdf")
        };
        var sites = new List<DiagnosticsEventsMutationSite>();

        foreach (var sourceRoot in sourceRoots)
        {
            foreach (var file in Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                var sourceRelativePath = Path.GetRelativePath(sourceRoot, file);
                if (IsBuildOutputPath(sourceRelativePath))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);
                for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    if (!DiagnosticsEventsMutationRegex.IsMatch(lines[lineIndex]))
                    {
                        continue;
                    }

                    sites.Add(new DiagnosticsEventsMutationSite(
                        NormalizePath(Path.GetRelativePath(root, file)),
                        lineIndex + 1));
                }
            }
        }

        return sites;
    }

    private static string ReadSource(params string[] pathSegments)
    {
        return File.ReadAllText(Path.Combine([FindRepoRoot(), .. pathSegments]));
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static bool IsBuildOutputPath(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Length > 0 &&
            (segments[0].Equals("bin", StringComparison.OrdinalIgnoreCase) ||
             segments[0].Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Html2x.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed record DiagnosticsEventsMutationSite(string RelativePath, int LineNumber);
}
