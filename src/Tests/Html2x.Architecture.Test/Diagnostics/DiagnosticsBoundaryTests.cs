using Html2x.Diagnostics.Contracts;
using Html2x.Diagnostics;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.Text;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Tables;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Style.Computation;
using Shouldly;
using Html2x.Architecture.Test.Support;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test.Diagnostics;

public sealed class DiagnosticsBoundaryTests
{
    [Fact]
    public void DiagnosticsBoundaryDocs_DefineProductionRulesAndOwnership()
    {
        DocumentAnchors.ArchitectureDiagnostics.DependencyDirection
            .ShouldMentionTopics(
                DocumentTopic.AssemblyOf<IDiagnosticsSink>(),
                DocumentTopic.AssemblyOf<DiagnosticsCollector>(),
                "Diagnostic producer modules");
        DocumentAnchors.ArchitectureDiagnostics.DiagnosticFieldsValueRules
            .ShouldMentionTopics(
                DocumentTopic.Type<DiagnosticFields>(),
                "object");
        DocumentAnchors.ArchitectureDiagnostics.RuntimeFlow
            .ShouldMentionTopics(
                DocumentTopic.Type<IDiagnosticsSink>(),
                DocumentTopic.Type<DiagnosticsCollector>(),
                DocumentTopic.Type<DiagnosticsReport>(),
                "Renderer diagnostics");
        DocumentAnchors.ArchitectureDiagnostics.RuntimeOwnership
            .ShouldMentionTopics(
                DocumentTopic.Type<DiagnosticsCollector>(),
                DocumentTopic.Type(typeof(DiagnosticsReportSerializer)),
                "producer-specific models");
        DocumentAnchors.ArchitectureDiagnostics.FacadeBoundary
            .ShouldMentionTopics(
                "Public facade options",
                DocumentTopic.AssemblyOf<IDiagnosticsSink>(),
                DocumentTopic.AssemblyOf<DiagnosticsCollector>());
        DocumentAnchors.ArchitectureDiagnostics.EmissionRule
            .ShouldMentionTopics(
                DocumentTopic.Text(nameof(IDiagnosticsSink) + "." + nameof(IDiagnosticsSink.Emit)),
                DocumentTopic.Type<DiagnosticRecord>());
    }

    [Fact]
    public void DiagnosticsContractsProject_IsIndependentAndInSolution()
    {
        Solution.Load(RepositoryLayout.SourceRoot, RepositoryLayout.SolutionFileName)
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
                nameof(TableBox),
                nameof(TableLayoutResult));
        SemanticProjectFor<IDiagnosticsSink>()
            .ShouldNotReferenceNamespaces(
                AssemblyName<LayoutPipeline>(),
                RendererNamespace,
                ParserPackageName(),
                ExternalPackageIds.SkiaSharp);
    }

    [Fact]
    public void StageProjects_ReferenceDiagnosticsContractsWithoutRuntime()
    {
        foreach (var project in StageProjects())
        {
            project.ShouldNotReferenceProjects(AssemblyName<DiagnosticsCollector>());
            project.ProjectReferences()
                .ShouldContain(AssemblyName<IDiagnosticsSink>());
        }
    }

    [Fact]
    public void PaginationDiagnostics_AreLocalToPaginationProject()
    {
        File.Exists(PathFromRoot(
                RepositoryLayout.SourceRoot,
                AssemblyName<LayoutPipeline>(),
                NamespaceSegmentOf(typeof(GeometrySnapshotDiagnostics)),
                nameof(PaginationDiagnostics) + RepositoryLayout.CSharpFileExtension))
            .ShouldBeFalse();

        var diagnostics = SourceFileFor(typeof(PaginationDiagnostics));
        diagnostics.ShouldDeclareNamespace(NamespaceOf(typeof(PaginationDiagnostics)));
        diagnostics.ShouldContainType(nameof(PaginationDiagnostics), InternalAccessibility);
        diagnostics.ShouldUseIdentifier(nameof(IDiagnosticsSink));
        diagnostics.ShouldContainStringLiteral(PaginationDiagnosticNames.Events.PageCreated);
        diagnostics.ShouldContainStringLiteral(PaginationDiagnosticNames.Events.BlockMovedNextPage);
        diagnostics.ShouldContainStringLiteral(PaginationDiagnosticNames.Events.OversizedBlock);
        diagnostics.ShouldContainStringLiteral(PaginationDiagnosticNames.Events.EmptyDocument);
    }

    [Fact]
    public void PipelineBoundaries_AcceptDiagnosticsSink()
    {
        SourceFileFor<HtmlConverter>()
            .ShouldUseIdentifier(nameof(HtmlConversionDiagnostics));
        SourceFileFor<HtmlConversionDiagnostics>()
            .ShouldConstructType(nameof(DiagnosticsCollector));
        SourceFileFor<HtmlToPdfResult>()
            .ShouldContainPropertyInType(
                nameof(HtmlToPdfResult),
                DiagnosticsReportTypeName,
                DiagnosticsReportTypeName + "?",
                PublicAccessibility);
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
        SourceFileFor<PdfRenderer>()
            .ShouldHaveParameter(nameof(PdfRenderer.Render), "diagnosticsSink", NullableTypeName<IDiagnosticsSink>());
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotReferenceStageParserOrRendererDependencies()
    {
        ProjectFor<DiagnosticsCollector>()
            .ShouldNotReferenceProjects(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<LayoutPaginator>(),
                AssemblyName<PdfRenderer>());
        ProjectFor<DiagnosticsCollector>()
            .ShouldNotReferencePackages(ExternalPackageIds.AngleSharp, ExternalPackageIds.SkiaSharp);

        SemanticProjectFor<DiagnosticsCollector>()
            .ShouldNotReferenceNamespaces(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<LayoutPaginator>(),
                AssemblyName<PdfRenderer>(),
                ParserPackageName(),
                ExternalPackageIds.SkiaSharp);
    }

    [Fact]
    public void DiagnosticsRuntime_DoesNotOwnProducerLocalStageNames()
    {
        SourceSetFor<DiagnosticsCollector>()
            .ShouldNotContainStringLiterals(
                LayoutStageNames.Pagination,
                PaginationDiagnosticNames.Stages.Pagination,
                GeometrySnapshotDiagnostics.EventName,
                StyleDiagnosticNames.Events.UnsupportedDeclaration,
                FontDiagnosticNames.Events.Resolve,
                ImageRenderDiagnosticNames.Events.Render);
    }

    [Fact]
    public void DiagnosticsRuntime_OwnsCollectorReportAndSerializer()
    {
        var collector = SourceFileFor<DiagnosticsCollector>();
        var report = SourceFileFor<DiagnosticsReport>();
        var serializer = SourceFileFor(typeof(DiagnosticsReportSerializer));

        collector.ShouldContainType<DiagnosticsCollector>(PublicAccessibility, true);
        collector.ShouldContainMethodInType(
            nameof(DiagnosticsCollector),
            nameof(DiagnosticsCollector.ToReport),
            DiagnosticsReportTypeName,
            PublicAccessibility);
        report.ShouldContainType<DiagnosticsReport>(PublicAccessibility, true);
        report.ShouldContainPropertyInType<DiagnosticsReport>(
            nameof(DiagnosticsReport.Records),
            ReadOnlyListTypeName<DiagnosticRecord>(),
            PublicAccessibility);
        serializer.ShouldContainType(typeof(DiagnosticsReportSerializer), PublicAccessibility);
        serializer.ShouldContainMethodInType(
            nameof(DiagnosticsReportSerializer),
            nameof(DiagnosticsReportSerializer.ToJson),
            CSharpTypeName<string>(),
            PublicAccessibility);
    }

    [Fact]
    public void DiagnosticsReportSerializer_ReferencesOnlyContractsAndReportTypes()
    {
        var serializer = SourceFileFor(typeof(DiagnosticsReportSerializer));

        serializer.ShouldUseNamespace(NamespaceOf<IDiagnosticsSink>());
        serializer.ShouldUseIdentifier(DiagnosticsReportTypeName);
        serializer.ShouldUseIdentifier(nameof(DiagnosticValue));
        serializer.ShouldNotUseIdentifiers(
            nameof(LayoutSnapshot),
            nameof(GeometrySnapshot),
            nameof(FragmentSnapshot),
            nameof(TableBox),
            nameof(TableLayoutResult));
        SemanticProjectFor<DiagnosticsCollector>()
            .ShouldNotReferenceNamespaces(AssemblyName<LayoutPipeline>(), RendererNamespace, ParserPackageName(),
                ExternalPackageIds.SkiaSharp);
    }

    private static IReadOnlyList<Project> StageProjects() =>
    [
        ProjectFor<StyleTreeBuilder>(),
        ProjectFor<LayoutGeometryConstruction>(),
        ProjectFor<LayoutPaginator>(),
        ProjectFor<LayoutPipeline>(),
        ProjectFor<PdfRenderer>()
    ];

    private static readonly string RendererNamespace = NamespacePrefix(
        NamespaceOf<PdfRenderer>(),
        2);

    private static readonly string DiagnosticsReportTypeName = TypeName<DiagnosticsReport>();
}
