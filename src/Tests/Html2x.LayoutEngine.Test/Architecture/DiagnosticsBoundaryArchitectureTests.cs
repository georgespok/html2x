using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class DiagnosticsBoundaryArchitectureTests
{
    [Fact]
    public void DiagnosticsBoundaryDocs_DefineProductionRulesAndOwnership()
    {
        ArchitectureDocument.Load("docs", "architecture", "diagnostics-boundary.md")
            .ShouldMentionTopicsInSection(
                "Dependency Direction",
                "Html2x.Diagnostics.Contracts",
                "Html2x.Diagnostics",
                "Diagnostic producer modules");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics-boundary.md")
            .ShouldMentionTopicsInSection(
                "DiagnosticFields Value Rules",
                "DiagnosticFields",
                "object");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics-boundary.md")
            .ShouldMentionTopicsInSection(
                "Runtime Flow",
                "IDiagnosticsSink",
                "DiagnosticsCollector",
                "DiagnosticsReport",
                "Renderer diagnostics");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics-boundary.md")
            .ShouldMentionTopicsInSection(
                "Runtime Ownership",
                "DiagnosticsCollector",
                "DiagnosticsReportSerializer",
                "producer-specific models");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics-boundary.md")
            .ShouldMentionTopicsInSection(
                "Facade Boundary",
                "Public facade options",
                "Html2x.Diagnostics.Contracts",
                "Html2x.Diagnostics");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics-boundary.md")
            .ShouldMentionTopicsInSection(
                "Emission Rule",
                "IDiagnosticsSink.Emit",
                "DiagnosticRecord");
    }

    [Fact]
    public void DiagnosticsContractsProject_IsIndependentAndInSolution()
    {
        ArchitectureSolution.Load("src", "Html2x.sln")
            .ProjectNames()
            .ShouldContain("Html2x.Diagnostics.Contracts");

        var project = ArchitectureProject.Load(
            "src",
            "Html2x.Diagnostics.Contracts",
            "Html2x.Diagnostics.Contracts.csproj");
        project.ShouldHaveNoProjectReferences();
        project.ShouldHaveNoPackageReferences();
    }

    [Fact]
    public void DiagnosticsContractsSource_StaysGeneric()
    {
        CSharpSourceSet.FromDirectory("src", "Html2x.Diagnostics.Contracts")
            .ShouldNotUseObjectType();
        CSharpSourceSet.FromDirectory("src", "Html2x.Diagnostics.Contracts")
            .ShouldNotUseIdentifiers(
                "IDiagnosticsPayload",
                "DiagnosticsSession",
                "DiagnosticsEvent",
                "Payload",
                "Snapshot",
                "TableBox",
                "TableLayoutResult");
        ArchitectureSemanticProject.Load(
                "src",
                "Html2x.Diagnostics.Contracts",
                "Html2x.Diagnostics.Contracts.csproj")
            .ShouldNotReferenceNamespaces(
                "Html2x.LayoutEngine",
                "Html2x.Renderers",
                ParserPackageName(),
                "SkiaSharp");
    }

    [Fact]
    public void StageProjects_ReferenceDiagnosticsContractsWithoutRuntime()
    {
        foreach (var project in StageProjects())
        {
            project.ShouldNotReferenceProjects("Html2x.Diagnostics");
            project.ProjectReferences()
                .ShouldContain("Html2x.Diagnostics.Contracts");
        }
    }

    [Fact]
    public void PaginationDiagnostics_AreLocalToPaginationProject()
    {
        File.Exists(PathFromRoot("src", "Html2x.LayoutEngine", "Diagnostics", "PaginationDiagnostics.cs"))
            .ShouldBeFalse();

        var diagnostics = CSharpSourceFile.Load(
            "src",
            "Html2x.LayoutEngine.Pagination",
            "PaginationDiagnostics.cs");
        diagnostics.ShouldDeclareNamespace("Html2x.LayoutEngine.Pagination");
        diagnostics.ShouldContainType("PaginationDiagnostics", "internal");
        diagnostics.ShouldUseIdentifier("IDiagnosticsSink");
        diagnostics.ShouldContainStringLiteral("layout/pagination/page-created");
        diagnostics.ShouldContainStringLiteral("layout/pagination/block-moved-next-page");
        diagnostics.ShouldContainStringLiteral("layout/pagination/oversized-block");
        diagnostics.ShouldContainStringLiteral("layout/pagination/empty-document");
    }

    [Fact]
    public void PipelineBoundaries_AcceptDiagnosticsSink()
    {
        CSharpSourceFile.Load("src", "Html2x", "HtmlConverter.cs")
            .ShouldConstructType("DiagnosticsCollector");
        CSharpSourceFile.Load("src", "Html2x", "Html2PdfResult.cs")
            .ShouldContainPropertyInType("Html2PdfResult", "DiagnosticsReport", "DiagnosticsReport?", "public");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine", "LayoutBuilder.cs")
            .ShouldHaveParameter("BuildAsync", "diagnosticsSink", "IDiagnosticsSink?");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Style", "StyleTreeBuilder.cs")
            .ShouldHaveParameter("BuildAsync", "diagnosticsSink", "IDiagnosticsSink?");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Style", "IStyleTreeBuilder.cs")
            .ShouldHaveParameter("BuildAsync", "diagnosticsSink", "IDiagnosticsSink?");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Geometry", "Geometry", "LayoutGeometryBuilder.cs")
            .ShouldHaveParameter("Build", "diagnosticsSink", "IDiagnosticsSink?");
        CSharpSourceFile.Load("src", "Html2x.LayoutEngine.Pagination", "LayoutPaginator.cs")
            .ShouldHaveParameter("Paginate", "diagnosticsSink", "IDiagnosticsSink?");
        CSharpSourceFile.Load("src", "Html2x.Renderers.Pdf", "Pipeline", "PdfRenderer.cs")
            .ShouldHaveParameter("RenderAsync", "diagnosticsSink", "IDiagnosticsSink?");
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotReferenceStageParserOrRendererDependencies()
    {
        ArchitectureProject.Load("src", "Html2x.Diagnostics", "Html2x.Diagnostics.csproj")
            .ShouldNotReferenceProjects(
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Pagination",
                "Html2x.Renderers.Pdf");
        ArchitectureProject.Load("src", "Html2x.Diagnostics", "Html2x.Diagnostics.csproj")
            .ShouldNotReferencePackages(ParserPackageName(), "SkiaSharp");

        ArchitectureSemanticProject.Load("src", "Html2x.Diagnostics", "Html2x.Diagnostics.csproj")
            .ShouldNotReferenceNamespaces(
                "Html2x.LayoutEngine",
                "Html2x.LayoutEngine.Pagination",
                "Html2x.Renderers.Pdf",
                ParserPackageName(),
                "SkiaSharp");
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotOwnProducerLocalStageNames()
    {
        CSharpSourceSet.FromDirectory("src", "Html2x.Diagnostics")
            .ShouldNotContainStringLiterals(
                "stage/pagination",
                "layout/pagination",
                "layout/geometry-snapshot",
                "style/unsupported-declaration",
                "font/resolve",
                "image/render");
    }

    [Fact]
    public void DiagnosticsRuntime_OwnsCollectorReportAndSerializer()
    {
        var collector = CSharpSourceFile.Load("src", "Html2x.Diagnostics", "DiagnosticsCollector.cs");
        var report = CSharpSourceFile.Load("src", "Html2x.Diagnostics", "DiagnosticsReport.cs");
        var serializer = CSharpSourceFile.Load("src", "Html2x.Diagnostics", "DiagnosticsReportSerializer.cs");

        collector.ShouldContainType("DiagnosticsCollector", "public", isSealed: true);
        collector.ShouldContainMethodInType("DiagnosticsCollector", "ToReport", "DiagnosticsReport", "public");
        report.ShouldContainType("DiagnosticsReport", "public", isSealed: true);
        report.ShouldContainPropertyInType("DiagnosticsReport", "Records", "IReadOnlyList<DiagnosticRecord>", "public");
        serializer.ShouldContainType("DiagnosticsReportSerializer", "public");
        serializer.ShouldContainMethodInType("DiagnosticsReportSerializer", "ToJson", "string", "public");
    }

    [Fact]
    public void DiagnosticsReportSerializer_ReferencesOnlyContractsAndReportTypes()
    {
        var serializer = CSharpSourceFile.Load("src", "Html2x.Diagnostics", "DiagnosticsReportSerializer.cs");

        serializer.ShouldUseNamespace("Html2x.Diagnostics.Contracts");
        serializer.ShouldUseIdentifier("DiagnosticsReport");
        serializer.ShouldUseIdentifier("DiagnosticValue");
        serializer.ShouldNotUseIdentifiers(
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
            "TableLayoutResult");
        ArchitectureSemanticProject.Load("src", "Html2x.Diagnostics", "Html2x.Diagnostics.csproj")
            .ShouldNotReferenceNamespaces("Html2x.LayoutEngine", "Html2x.Renderers", ParserPackageName(), "SkiaSharp");
    }

    [Fact]
    public void DiagnosticsCollections_AreNotMutatedDirectly()
    {
        foreach (var sourceRoot in new[]
        {
            CSharpSourceSet.FromDirectory("src", "Html2x"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Pagination"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Style"),
            CSharpSourceSet.FromDirectory("src", "Html2x.LayoutEngine.Geometry"),
            CSharpSourceSet.FromDirectory("src", "Html2x.Renderers.Pdf")
        })
        {
            sourceRoot.ShouldNotInvokeMemberOn("Events", "Add", "AddRange", "Clear", "Remove", "RemoveAt", "Insert");
        }
    }

    [Fact]
    public void ObsoleteAbstractionsDiagnosticsTypes_AreRemoved()
    {
        Directory.Exists(PathFromRoot("src", "Html2x.Abstractions"))
            .ShouldBeFalse("diagnostics cannot leak into a deleted obsolete module.");
        ArchitectureSolution.Load("src", "Html2x.sln")
            .ProjectNames()
            .ShouldNotContain("Html2x.Abstractions");
    }

    private static IReadOnlyList<ArchitectureProject> StageProjects() =>
    [
        ArchitectureProject.Load("src", "Html2x.LayoutEngine.Style", "Html2x.LayoutEngine.Style.csproj"),
        ArchitectureProject.Load("src", "Html2x.LayoutEngine.Geometry", "Html2x.LayoutEngine.Geometry.csproj"),
        ArchitectureProject.Load("src", "Html2x.LayoutEngine.Pagination", "Html2x.LayoutEngine.Pagination.csproj"),
        ArchitectureProject.Load("src", "Html2x.LayoutEngine", "Html2x.LayoutEngine.csproj"),
        ArchitectureProject.Load("src", "Html2x.Renderers.Pdf", "Html2x.Renderers.Pdf.csproj")
    ];

    private static string PathFromRoot(params string[] pathSegments) =>
        ArchitecturePaths.PathFromRoot(pathSegments);

    private static string ParserPackageName() => "Angle" + "Sharp";
}
