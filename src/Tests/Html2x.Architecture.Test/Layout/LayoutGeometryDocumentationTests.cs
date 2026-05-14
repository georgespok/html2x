using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Geometry.Publishing;
using Html2x.LayoutEngine.Geometry.Writing;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Geometry;
using Html2x.Resources;
using Html2x.Architecture.Test.Support;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test.Layout;

public sealed class LayoutGeometryDocumentationTests
{
    [Fact]
    public void Docs_RecordCurrentModuleOwnership()
    {
        DocumentAnchors.ArchitecturePipeline.Composition
            .ShouldMentionTopics(
                DocumentTopic.AssemblyOf<StyleNode>(),
                DocumentTopic.AssemblyOf<StyleTreeBuilder>(),
                DocumentTopic.AssemblyOf<BlockBox>(),
                DocumentTopic.AssemblyOf<FragmentTreeBuilder>(),
                DocumentTopic.AssemblyOf<LayoutPaginator>(),
                DocumentTopic.AssemblyOf<ImageResourceStore>());
        DocumentAnchors.ArchitecturePipeline.FragmentTreeBuilding
            .ShouldMentionTopics(
                DocumentTopic.Type<PublishedLayoutTree>(),
                DocumentTopic.Type<FragmentTree>());
        DocumentAnchors.ArchitectureModuleBoundaries.OwnershipMatrix
            .ShouldMentionTopics(
                DocumentTopic.AssemblyOf<StyleNode>(),
                DocumentTopic.AssemblyOf<FragmentTreeBuilder>(),
                DocumentTopic.AssemblyOf<LayoutPaginator>());
        DocumentAnchors.ArchitectureModuleBoundaries.ContractsStage
            .ShouldMentionTopics(
                DocumentTopic.NamespaceOf<BlockBox>(),
                DocumentTopic.NamespaceOf<PublishedLayoutTree>());
        DocumentAnchors.InternalGeometry.HelperOwnership
            .ShouldMentionTopics(
                DocumentTopic.AssemblyOf<StyleNode>(),
                DocumentTopic.Type<RectPt>(),
                DocumentTopic.Type<PointPt>(),
                DocumentTopic.Type<UsedGeometry>(),
                DocumentTopic.Type<PageContentArea>());
        DocumentAnchors.InternalGeometry.BlockFlowLocality
            .ShouldMentionTopics(
                DocumentTopic.Type<BlockFlowLayout>(),
                DocumentTopic.Type<BlockFlowMeasurement>(),
                DocumentTopic.Type<BlockLayoutRuleSet>(),
                DocumentTopic.Type<LayoutBoxStateWriter>(),
                DocumentTopic.Type<PublishedLayoutWriter>());
    }

    [Fact]
    public void TestingDocs_RecordFocusedTestOwnership()
    {
        DocumentAnchors.DevelopmentTestingStrategy.TestProjects
            .ShouldMentionTopics(
                DocumentTopic.Text(TestAssemblyNameFor<StyleTreeBuilder>()),
                DocumentTopic.Text(TestAssemblyNameFor<FragmentTreeBuilder>()),
                DocumentTopic.Text(TestAssemblyNameFor<LayoutPaginator>()));
        DocumentAnchors.DevelopmentTestingStrategy.OwnershipRules
            .ShouldMentionTopics(
                DocumentTopic.Text("Geometry tests must not reference " + ParserPackageName()),
                DocumentTopic.Type<PublishedLayoutTree>(),
                DocumentTopic.Type<PaginationResult>());
        DocumentAnchors.InternalPagination.ModuleSeam
            .ShouldMentionTopics(
                DocumentTopic.Type<LayoutPaginator>(),
                DocumentTopic.Type<PaginationOptions>(),
                DocumentTopic.Type<PaginationResult>(),
                DocumentTopic.Type<HtmlLayout>());
        DocumentAnchors.ReferenceDiagnosticsEvents.Pagination
            .ShouldMentionTopics(
                DocumentTopic.Constant(PaginationDiagnosticNames.Stages.Pagination),
                DocumentTopic.Constant(PaginationDiagnosticNames.Events.PageCreated));
    }
}
