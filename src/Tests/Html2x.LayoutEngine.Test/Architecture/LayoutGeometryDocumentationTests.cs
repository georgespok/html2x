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
                "Html2x.LayoutEngine.Contracts",
                "Html2x.LayoutEngine.Style",
                "Html2x.LayoutEngine.Geometry",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Pagination",
                "Html2x.Resources");
        ArchitectureDocument.Load("docs", "architecture", "pipeline.md")
            .ShouldMentionTopicsInSection("Fragment Projection", "PublishedLayoutTree", "FragmentTree");
        ArchitectureDocument.Load("docs", "architecture", "stage-ownership.md")
            .ShouldMentionTopicsInSection(
                "Ownership Matrix",
                "Html2x.LayoutEngine.Contracts",
                "Html2x.LayoutEngine.Fragments",
                "Html2x.LayoutEngine.Pagination");
        ArchitectureDocument.Load("docs", "architecture", "stage-ownership.md")
            .ShouldMentionTopicsInSection(
                "Contracts Stage",
                "Html2x.LayoutEngine.Geometry.Models",
                "Html2x.LayoutEngine.Contracts.Published");
        ArchitectureDocument.Load("docs", "architecture", "geometry.md")
            .ShouldMentionTopicsInSection(
                "Helper Ownership",
                "Html2x.LayoutEngine.Contracts",
                "RectPt",
                "PointPt",
                "UsedGeometry",
                "PageContentArea");
        ArchitectureDocument.Load("docs", "architecture", "geometry.md")
            .ShouldMentionTopicsInSection(
                "Block Flow Locality",
                "BlockFlowLayoutExecutor",
                "BlockFlowMeasurementExecutor");
    }

    [Fact]
    public void TestingDocs_RecordFocusedTestOwnership()
    {
        ArchitectureDocument.Load("docs", "development", "testing.md")
            .ShouldMentionTopicsInSection(
                "Test Projects",
                "Html2x.LayoutEngine.Style.Test",
                "Html2x.LayoutEngine.Fragments.Test",
                "Html2x.LayoutEngine.Pagination.Test");
        ArchitectureDocument.Load("docs", "development", "testing.md")
            .ShouldMentionTopicsInSection(
                "Ownership Rules",
                "Geometry tests must not reference " + ParserPackageName(),
                "PublishedLayoutTree",
                "PaginationResult");
        ArchitectureDocument.Load("docs", "internals", "pagination.md")
            .ShouldMentionTopicsInSection(
                "Module Seam",
                "LayoutPaginator",
                "PaginationOptions",
                "PaginationResult",
                "HtmlLayout");
        ArchitectureDocument.Load("docs", "reference", "diagnostics-events.md")
            .ShouldMentionTopicsInSection(
                "Pagination",
                "stage/pagination",
                "layout/pagination/page-created");
    }
}
