using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Shouldly;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class DiagnosticsBoundaryArchitectureTests
{
    [Fact]
    public void DiagnosticsBoundaryDocs_DefineProductionRulesAndOwnership()
    {
        ArchitectureDocument.Load("docs", "architecture", "diagnostics.md")
            .ShouldMentionTopicsInSection(
                "Dependency Direction",
                AssemblyName<IDiagnosticsSink>(),
                DiagnosticsAssemblyName,
                "Diagnostic producer modules");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics.md")
            .ShouldMentionTopicsInSection(
                "DiagnosticFields Value Rules",
                nameof(DiagnosticFields),
                "object");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics.md")
            .ShouldMentionTopicsInSection(
                "Runtime Flow",
                nameof(IDiagnosticsSink),
                "DiagnosticsCollector",
                "DiagnosticsReport",
                "Renderer diagnostics");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics.md")
            .ShouldMentionTopicsInSection(
                "Runtime Ownership",
                "DiagnosticsCollector",
                "DiagnosticsReportSerializer",
                "producer-specific models");
        ArchitectureDocument.Load("docs", "architecture", "diagnostics.md")
            .ShouldMentionTopicsInSection(
                "Facade Boundary",
                "Public facade options",
                AssemblyName<IDiagnosticsSink>(),
                DiagnosticsAssemblyName);
        ArchitectureDocument.Load("docs", "architecture", "diagnostics.md")
            .ShouldMentionTopicsInSection(
                "Emission Rule",
                nameof(IDiagnosticsSink) + "." + nameof(IDiagnosticsSink.Emit),
                nameof(DiagnosticRecord));
    }

    [Fact]
    public void DiagnosticsContractsProject_IsIndependentAndInSolution()
    {
        ArchitectureSolution.Load("src", "Html2x.sln")
            .ProjectNames()
            .ShouldContain(AssemblyName<IDiagnosticsSink>());

        var project = ProjectFor<IDiagnosticsSink>();
        project.ShouldHaveNoProjectReferences();
        project.ShouldHaveNoPackageReferences();
    }

    [Fact]
    public void DiagnosticsContractsSource_StaysGeneric()
    {
        SourceSetFor<IDiagnosticsSink>()
            .ShouldNotUseObjectType();
        SourceSetFor<IDiagnosticsSink>()
            .ShouldNotUseIdentifiers(
                "IDiagnosticsPayload",
                "DiagnosticsSession",
                "DiagnosticsEvent",
                "Payload",
                "Snapshot",
                "TableBox",
                "TableLayoutResult");
        SemanticProjectFor<IDiagnosticsSink>()
            .ShouldNotReferenceNamespaces(
                AssemblyName<LayoutPipeline>(),
                "Html2x.Renderers",
                ParserPackageName(),
                SkiaSharpPackageName);
    }

    [Fact]
    public void StageProjects_ReferenceDiagnosticsContractsWithoutRuntime()
    {
        foreach (var project in StageProjects())
        {
            project.ShouldNotReferenceProjects(DiagnosticsAssemblyName);
            project.ProjectReferences()
                .ShouldContain(AssemblyName<IDiagnosticsSink>());
        }
    }

    [Fact]
    public void PaginationDiagnostics_AreLocalToPaginationProject()
    {
        File.Exists(PathFromRoot("src", AssemblyName<LayoutPipeline>(), "Diagnostics", "PaginationDiagnostics.cs"))
            .ShouldBeFalse();

        var diagnostics = SourceFileFor(typeof(PaginationDiagnostics));
        diagnostics.ShouldDeclareNamespace(NamespaceOf(typeof(PaginationDiagnostics)));
        diagnostics.ShouldContainType(nameof(PaginationDiagnostics), InternalAccessibility);
        diagnostics.ShouldUseIdentifier(nameof(IDiagnosticsSink));
        diagnostics.ShouldContainStringLiteral("layout/pagination/page-created");
        diagnostics.ShouldContainStringLiteral("layout/pagination/block-moved-next-page");
        diagnostics.ShouldContainStringLiteral("layout/pagination/oversized-block");
        diagnostics.ShouldContainStringLiteral("layout/pagination/empty-document");
    }

    [Fact]
    public void PipelineBoundaries_AcceptDiagnosticsSink()
    {
        CSharpSourceFile.Load("src", FacadeAssemblyName, "HtmlConverter.cs")
            .ShouldUseIdentifier("HtmlConversionDiagnostics");
        CSharpSourceFile.Load("src", FacadeAssemblyName, "HtmlConversionDiagnostics.cs")
            .ShouldConstructType("DiagnosticsCollector");
        CSharpSourceFile.Load("src", FacadeAssemblyName, "HtmlToPdfResult.cs")
            .ShouldContainPropertyInType("HtmlToPdfResult", "DiagnosticsReport", "DiagnosticsReport?", "public");
        SourceFileFor<LayoutPipeline>()
            .ShouldHaveParameter(nameof(LayoutPipeline.BuildAsync), "diagnosticsSink",
                NullableTypeName<IDiagnosticsSink>());
        SourceFileFor<StyleTreeBuilder>()
            .ShouldHaveParameter(nameof(StyleTreeBuilder.BuildAsync), "diagnosticsSink",
                NullableTypeName<IDiagnosticsSink>());
        SourceFileFor<LayoutGeometryConstruction>()
            .ShouldHaveParameter(nameof(LayoutGeometryConstruction.Build), "diagnosticsSink",
                NullableTypeName<IDiagnosticsSink>());
        SourceFileFor<LayoutPaginator>()
            .ShouldHaveParameter(nameof(LayoutPaginator.Paginate), "diagnosticsSink",
                NullableTypeName<IDiagnosticsSink>());
        CSharpSourceFile.Load("src", PdfRendererAssemblyName, "Pipeline", "PdfRenderer.cs")
            .ShouldHaveParameter("Render", "diagnosticsSink", NullableTypeName<IDiagnosticsSink>());
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotReferenceStageParserOrRendererDependencies()
    {
        ArchitectureProject.Load("src", DiagnosticsAssemblyName, DiagnosticsAssemblyName + ".csproj")
            .ShouldNotReferenceProjects(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<LayoutPaginator>(),
                PdfRendererAssemblyName);
        ArchitectureProject.Load("src", DiagnosticsAssemblyName, DiagnosticsAssemblyName + ".csproj")
            .ShouldNotReferencePackages(ParserPackageName(), SkiaSharpPackageName);

        ArchitectureSemanticProject.Load("src", DiagnosticsAssemblyName, DiagnosticsAssemblyName + ".csproj")
            .ShouldNotReferenceNamespaces(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<LayoutPaginator>(),
                PdfRendererAssemblyName,
                ParserPackageName(),
                SkiaSharpPackageName);
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotOwnProducerLocalStageNames()
    {
        CSharpSourceSet.FromDirectory("src", DiagnosticsAssemblyName)
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
        var collector = CSharpSourceFile.Load("src", DiagnosticsAssemblyName, "DiagnosticsCollector.cs");
        var report = CSharpSourceFile.Load("src", DiagnosticsAssemblyName, "DiagnosticsReport.cs");
        var serializer = CSharpSourceFile.Load("src", DiagnosticsAssemblyName, "DiagnosticsReportSerializer.cs");

        collector.ShouldContainType("DiagnosticsCollector", "public", true);
        collector.ShouldContainMethodInType("DiagnosticsCollector", "ToReport", "DiagnosticsReport", "public");
        report.ShouldContainType("DiagnosticsReport", "public", true);
        report.ShouldContainPropertyInType("DiagnosticsReport", "Records", "IReadOnlyList<DiagnosticRecord>", "public");
        serializer.ShouldContainType("DiagnosticsReportSerializer", "public");
        serializer.ShouldContainMethodInType("DiagnosticsReportSerializer", "ToJson", "string", "public");
    }

    [Fact]
    public void DiagnosticsReportSerializer_ReferencesOnlyContractsAndReportTypes()
    {
        var serializer = CSharpSourceFile.Load("src", DiagnosticsAssemblyName, "DiagnosticsReportSerializer.cs");

        serializer.ShouldUseNamespace(NamespaceOf<IDiagnosticsSink>());
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
        ArchitectureSemanticProject.Load("src", DiagnosticsAssemblyName, DiagnosticsAssemblyName + ".csproj")
            .ShouldNotReferenceNamespaces(AssemblyName<LayoutPipeline>(), "Html2x.Renderers", ParserPackageName(),
                SkiaSharpPackageName);
    }

    [Fact]
    public void DiagnosticsCollections_AreNotMutatedDirectly()
    {
        foreach (var sourceRoot in new[]
                 {
                     CSharpSourceSet.FromDirectory("src", FacadeAssemblyName),
                     SourceSetFor<LayoutPipeline>(),
                     SourceSetFor<LayoutPaginator>(),
                     SourceSetFor<StyleTreeBuilder>(),
                     SourceSetFor<LayoutGeometryConstruction>(),
                     CSharpSourceSet.FromDirectory("src", PdfRendererAssemblyName)
                 })
        {
            sourceRoot.ShouldNotInvokeMemberOn("Events", "Add", "AddRange", "Clear", "Remove", "RemoveAt", "Insert");
        }
    }

    private static IReadOnlyList<ArchitectureProject> StageProjects() =>
    [
        ProjectFor<StyleTreeBuilder>(),
        ProjectFor<LayoutGeometryConstruction>(),
        ProjectFor<LayoutPaginator>(),
        ProjectFor<LayoutPipeline>(),
        ArchitectureProject.Load("src", PdfRendererAssemblyName, PdfRendererAssemblyName + ".csproj")
    ];
}
