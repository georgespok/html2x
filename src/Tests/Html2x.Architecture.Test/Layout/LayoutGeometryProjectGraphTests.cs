using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Stage.Contracts.Geometry;
using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Documents;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.Resources;
using Html2x.Text;
using Html2x.Architecture.Test.Support;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test.Layout;

public sealed class LayoutGeometryProjectGraphTests
{
    [Fact]
    public void Solution_ContainsGuardrailProjects()
    {
        Solution.Load("src", "Html2x.sln")
            .ProjectNames()
            .ShouldContainSet([
                AssemblyName<StyleNode>(),
                AssemblyName<ILayoutGeometryStage>(),
                AssemblyName<HtmlLayout>(),
                AssemblyName<FragmentTreeBuilder>(),
                AssemblyName<LayoutPaginator>(),
                TestAssemblyNameFor<LayoutPaginator>(),
                TestAssemblyNameFor<FragmentTreeBuilder>(),
                TestAssemblyNameFor<StyleTreeBuilder>(),
                CurrentAssemblyName()
            ]);
    }

    [Fact]
    public void ProductionProjectGraph_FollowsOwnedModuleDirection()
    {
        ProjectFor<LayoutPipeline>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(),
                AssemblyName<FragmentTreeBuilder>(), AssemblyName<LayoutGeometryConstruction>(), AssemblyName<LayoutPaginator>(),
                AssemblyName<ILayoutGeometryStage>(), AssemblyName<StyleTreeBuilder>(), AssemblyName<HtmlLayout>(),
                AssemblyName<ITextMeasurer>());
        ProjectFor<StyleTreeBuilder>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(),
                AssemblyName<HtmlLayout>());
        ProjectFor<StyleTreeBuilder>()
            .ShouldReferencePackages(ExternalPackageIds.AngleSharp, ExternalPackageIds.AngleSharpCss);
        ProjectFor<LayoutGeometryConstruction>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(),
                AssemblyName<ILayoutGeometryStage>(), AssemblyName<HtmlLayout>(), AssemblyName<ITextMeasurer>());
        ProjectFor<LayoutPaginator>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>(),
                AssemblyName<HtmlLayout>());
        ProjectFor<LayoutPaginator>()
            .ShouldHaveNoPackageReferences();
        ProjectFor<StyleNode>()
            .ShouldReferenceProjects(AssemblyName<HtmlLayout>());
        ProjectFor<StyleNode>()
            .ShouldHaveNoPackageReferences();
        ProjectFor<ILayoutGeometryStage>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<StyleNode>());
        ProjectFor<ILayoutGeometryStage>()
            .ShouldHaveNoPackageReferences();
        ProjectFor<HtmlLayout>()
            .ShouldHaveNoProjectReferences();
        ProjectFor<HtmlLayout>()
            .ShouldHaveNoPackageReferences();
        ProjectFor<FragmentTreeBuilder>()
            .ShouldReferenceProjects(AssemblyName<StyleNode>(), AssemblyName<HtmlLayout>());
        ProjectFor<ITextMeasurer>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<HtmlLayout>());
        ProjectFor<ITextMeasurer>()
            .ShouldReferencePackages(ExternalPackageIds.SkiaSharp, ExternalPackageIds.SkiaSharpHarfBuzz);
    }

    [Fact]
    public void SharedContractsProject_DoesNotReferenceImplementationStages()
    {
        var contracts = SemanticProjectFor<StyleNode>();
        var stageContracts = SemanticProjectFor<ILayoutGeometryStage>();

        ProjectFor<StyleNode>()
            .ShouldReferenceProjects(AssemblyName<HtmlLayout>());
        contracts.ShouldNotReferenceNamespaces(
            NamespaceOf<StyleTreeBuilder>(),
            NamespaceOf<LayoutGeometryConstruction>(),
            NamespaceOf<FragmentTreeBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            RendererNamespace,
            ParserPackageName(),
            ExternalPackageIds.SkiaSharp);
        stageContracts.ShouldNotReferenceNamespaces(
            NamespaceOf<StyleTreeBuilder>(),
            NamespaceOf<LayoutGeometryConstruction>(),
            NamespaceOf<FragmentTreeBuilder>(),
            NamespaceOf<LayoutPaginator>(),
            RendererNamespace,
            ParserPackageName(),
            ExternalPackageIds.SkiaSharp);
    }

    [Fact]
    public void RendererProjectGraph_StaysIndependentFromLayoutStages()
    {
        var renderer = ProjectFor<PdfRenderer>();

        renderer.ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<HtmlLayout>(),
            AssemblyName<ImageResourceStore>(), AssemblyName<ITextMeasurer>());
        renderer.ShouldNotReferenceProjects(
            AssemblyName<LayoutPipeline>(),
            AssemblyName<StyleNode>(),
            AssemblyName<FragmentTreeBuilder>(),
            AssemblyName<LayoutGeometryConstruction>(),
            AssemblyName<StyleTreeBuilder>());
    }

    [Fact]
    public void FocusedTestProjects_StayInOwningModules()
    {
        TestProjectFor<LayoutPaginator>()
            .ShouldReferenceProjects(AssemblyName<IDiagnosticsSink>(), AssemblyName<LayoutPaginator>(),
                AssemblyName<HtmlLayout>());
        TestProjectFor<LayoutPaginator>()
            .ShouldNotReferenceProjects(
                AssemblyName<LayoutPipeline>(),
                AssemblyName<FragmentTreeBuilder>(),
                AssemblyName<LayoutGeometryConstruction>(),
                AssemblyName<StyleTreeBuilder>(),
                AssemblyName<PdfRenderer>(),
                AssemblyName<ITextMeasurer>());
        TestProjectFor<StyleTreeBuilder>()
            .ShouldNotReferenceProjects(AssemblyName<LayoutPipeline>(), AssemblyName<LayoutGeometryConstruction>());
        TestProjectFor<LayoutGeometryConstruction>()
            .ShouldNotReferencePackages(ExternalPackageIds.AngleSharp, ExternalPackageIds.AngleSharpCss);
    }

    private static readonly string RendererNamespace = NamespacePrefix(
        NamespaceOf<PdfRenderer>(),
        2);
}
