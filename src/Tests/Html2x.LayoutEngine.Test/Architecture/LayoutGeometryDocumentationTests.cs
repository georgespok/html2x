using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Box.Publishing;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Geometry;
using static Html2x.LayoutEngine.Test.Architecture.ArchitectureTestSupport;

namespace Html2x.LayoutEngine.Test.Architecture;

public sealed class LayoutGeometryDocumentationTests
{
    [Fact]
    public void ArchitectureDocs_RecordCurrentModuleOwnership()
    {
        ArchitectureDocument.Load("docs", "architecture", "pipeline.md")
            .ShouldMentionTopicsInSection(
                "Composition",
                AssemblyName<StyleNode>(),
                AssemblyName<StyleTreeBuilder>(),
                AssemblyName<BlockBox>(),
                AssemblyName<FragmentBuilder>(),
                AssemblyName<LayoutPaginator>(),
                ResourcesAssemblyName);
        ArchitectureDocument.Load("docs", "architecture", "pipeline.md")
            .ShouldMentionTopicsInSection("Fragment Projection", nameof(PublishedLayoutTree), nameof(FragmentTree));
        ArchitectureDocument.Load("docs", "architecture", "stage-ownership.md")
            .ShouldMentionTopicsInSection(
                "Ownership Matrix",
                AssemblyName<StyleNode>(),
                AssemblyName<FragmentBuilder>(),
                AssemblyName<LayoutPaginator>());
        ArchitectureDocument.Load("docs", "architecture", "stage-ownership.md")
            .ShouldMentionTopicsInSection(
                "Contracts Stage",
                NamespaceOf<BlockBox>(),
                NamespaceOf<PublishedLayoutTree>());
        ArchitectureDocument.Load("docs", "architecture", "geometry.md")
            .ShouldMentionTopicsInSection(
                "Helper Ownership",
                AssemblyName<StyleNode>(),
                nameof(RectPt),
                nameof(PointPt),
                nameof(UsedGeometry),
                nameof(PageContentArea));
        ArchitectureDocument.Load("docs", "architecture", "geometry.md")
            .ShouldMentionTopicsInSection(
                "Block Flow Locality",
                nameof(BlockFlowLayoutExecutor),
                nameof(BlockFlowMeasurementExecutor),
                nameof(BlockLayoutRuleSet),
                nameof(LayoutBoxStateWriter),
                nameof(PublishedLayoutWriter));
    }

    [Fact]
    public void TestingDocs_RecordFocusedTestOwnership()
    {
        ArchitectureDocument.Load("docs", "development", "testing.md")
            .ShouldMentionTopicsInSection(
                "Test Projects",
                TestAssemblyNameFor<StyleTreeBuilder>(),
                TestAssemblyNameFor<FragmentBuilder>(),
                TestAssemblyNameFor<LayoutPaginator>());
        ArchitectureDocument.Load("docs", "development", "testing.md")
            .ShouldMentionTopicsInSection(
                "Ownership Rules",
                "Geometry tests must not reference " + ParserPackageName(),
                nameof(PublishedLayoutTree),
                nameof(PaginationResult));
        ArchitectureDocument.Load("docs", "internals", "pagination.md")
            .ShouldMentionTopicsInSection(
                "Module Seam",
                nameof(LayoutPaginator),
                nameof(PaginationOptions),
                nameof(PaginationResult),
                nameof(HtmlLayout));
        ArchitectureDocument.Load("docs", "reference", "diagnostics-events.md")
            .ShouldMentionTopicsInSection(
                "Pagination",
                "stage/pagination",
                "layout/pagination/page-created");
    }
}
