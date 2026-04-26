using AngleSharp.Dom;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Moq;
using Shouldly;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Test;

public class LayoutBuilderTests
{
    private static readonly string[] LayoutStageNames =
    [
        "stage/dom",
        "stage/style",
        "stage/display-tree",
        "stage/layout-geometry",
        "stage/layout-validation",
        "stage/fragment-projection",
        "stage/pagination"
    ];

    private readonly Mock<IBoxTreeBuilder> _boxTreeBuilder;
    private readonly LayoutBuilder _builder;

    private readonly Mock<IDomProvider> _domProvider;
    private readonly Mock<IFragmentBuilder> _fragmentBuilder;
    private readonly Mock<IStyleComputer> _styleComputer;
    private readonly Mock<IImageProvider> _imageProvider;
    private readonly Mock<ITextMeasurer> _textMeasurer;
    private readonly Mock<IFontSource> _fontSource;

    public LayoutBuilderTests()
    {
        _domProvider = new Mock<IDomProvider>();
        _styleComputer = new Mock<IStyleComputer>();
        _boxTreeBuilder = new Mock<IBoxTreeBuilder>();
        _fragmentBuilder = new Mock<IFragmentBuilder>();
        _imageProvider = new Mock<IImageProvider>();
        _textMeasurer = new Mock<ITextMeasurer>();
        _fontSource = new Mock<IFontSource>();

        _builder = new LayoutBuilder(_domProvider.Object, _styleComputer.Object, _boxTreeBuilder.Object,
            _fragmentBuilder.Object, _imageProvider.Object, _textMeasurer.Object, _fontSource.Object);
    }

    [Fact]
    public async Task BuildAsync_PipelineSucceeds_ReturnPaginatedLayout()
    {
        // Arrange
        const string html = "<p>ignored by unit test</p>";

        var document = new Mock<IDocument>().Object;
        var displayRoot = new BlockBox(DisplayRole.Block);
        var styleTree = new StyleTree
            { Page = { Margin = new Spacing(10, 11, 12, 13) } };
        var boxTree = new BoxTree
        {
            Page =
            {
                Margin = new Spacing(10, 11, 12, 13)
            }
        };

        var expectedBlock = new BlockFragment();
        var fragmentTree = new FragmentTree();
        fragmentTree.Blocks.Add(expectedBlock);

        SetupSuccessfulPipeline(html, document, displayRoot, styleTree, boxTree, fragmentTree);

        var options = new LayoutOptions
        {
            PageSize = PaperSizes.Letter
        };

        // Act
        var layout = await _builder.BuildAsync(html, options);

        // Assert
        Assert.NotNull(layout);
        Assert.IsType<HtmlLayout>(layout);
        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Margins.Top.ShouldBe(10);
        layout.Pages[0].Margins.Right.ShouldBe(11);
        layout.Pages[0].Margins.Bottom.ShouldBe(12);
        layout.Pages[0].Margins.Left.ShouldBe(13);
        var pageBlock = layout.Pages[0].Children[0].ShouldBeOfType<BlockFragment>();
        pageBlock.ShouldNotBeSameAs(expectedBlock);
        pageBlock.FragmentId.ShouldBe(expectedBlock.FragmentId);
        pageBlock.Rect.ShouldBe(expectedBlock.Rect);

        _domProvider.Verify(x => x.LoadAsync(html, options), Times.Once);
        _styleComputer.Verify(x => x.Compute(document, It.IsAny<DiagnosticsSession?>()), Times.Once);
        _boxTreeBuilder.Verify(
            x => x.BuildDisplayTree(styleTree, It.IsAny<DiagnosticsSession?>()),
            Times.Once);
        _boxTreeBuilder.Verify(
            x => x.BuildLayoutGeometry(displayRoot, styleTree, It.IsAny<DiagnosticsSession?>(), It.IsAny<BoxTreeBuildContext?>()),
            Times.Once);
        _fragmentBuilder.Verify(x => x.Build(boxTree, It.IsAny<FragmentBuildContext>()), Times.Once);
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSessionIsProvided_PublishStageEvents()
    {
        const string html = "<p>ignored by unit test</p>";

        var document = new Mock<IDocument>().Object;
        var displayRoot = new BlockBox(DisplayRole.Block);
        var styleTree = new StyleTree();
        var boxTree = new BoxTree();
        var fragmentTree = new FragmentTree();
        fragmentTree.Blocks.Add(new BlockFragment());

        SetupSuccessfulPipeline(html, document, displayRoot, styleTree, boxTree, fragmentTree);

        var diagnosticsSession = new DiagnosticsSession();

        await _builder.BuildAsync(html, new LayoutOptions(), diagnosticsSession);

        var eventNames = diagnosticsSession.Events.Select(static e => e.Name).ToList();
        foreach (var stageName in LayoutStageNames)
        {
            eventNames.ShouldContain(stageName);
        }
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSessionIsProvided_PublishGeometrySnapshotPayload()
    {
        const string html = "<p>ignored by unit test</p>";

        var document = new Mock<IDocument>().Object;
        var displayRoot = new BlockBox(DisplayRole.Block);
        var styleTree = new StyleTree();
        var boxTree = new BoxTree();
        boxTree.Blocks.Add(new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(new System.Drawing.RectangleF(10f, 20f, 100f, 40f), new Spacing(), new Spacing())
        });

        var fragmentTree = new FragmentTree();
        fragmentTree.Blocks.Add(new BlockFragment
        {
            FragmentId = 7,
            PageNumber = 1,
            Rect = new System.Drawing.RectangleF(10f, 20f, 100f, 40f),
            DisplayRole = FragmentDisplayRole.Block,
            Style = new VisualStyle()
        });

        SetupSuccessfulPipeline(html, document, displayRoot, styleTree, boxTree, fragmentTree);

        var diagnosticsSession = new DiagnosticsSession();

        await _builder.BuildAsync(html, new LayoutOptions { PageSize = PaperSizes.Letter }, diagnosticsSession);

        var geometryEvent = diagnosticsSession.Events
            .SingleOrDefault(static e => e.Name == "layout/geometry-snapshot");

        geometryEvent.ShouldNotBeNull();
        geometryEvent!.Payload.ShouldBeOfType<GeometrySnapshotPayload>();

        var payload = (GeometrySnapshotPayload)geometryEvent.Payload!;
        payload.Snapshot.Boxes.Count.ShouldBe(1);
        payload.Snapshot.Fragments.PageCount.ShouldBe(1);
        payload.Snapshot.Pagination.Count.ShouldBe(1);
        payload.Snapshot.Pagination[0].Placements.ShouldHaveSingleItem().FragmentId.ShouldBe(7);
    }

    [Fact]
    public async Task BuildAsync_DiagnosticsSession_PublishesLifecycleEvents()
    {
        const string html = "<p>ignored by unit test</p>";

        var document = new Mock<IDocument>().Object;
        var displayRoot = new BlockBox(DisplayRole.Block);
        var styleTree = new StyleTree();
        var boxTree = new BoxTree();
        var fragmentTree = new FragmentTree();
        fragmentTree.Blocks.Add(new BlockFragment());

        SetupSuccessfulPipeline(html, document, displayRoot, styleTree, boxTree, fragmentTree);

        var diagnosticsSession = new DiagnosticsSession();

        await _builder.BuildAsync(html, new LayoutOptions(), diagnosticsSession);

        foreach (var stageName in LayoutStageNames)
        {
            diagnosticsSession.Events.ShouldContain(e =>
                e.Name == stageName &&
                e.Type == DiagnosticsEventType.StartStage &&
                e.StageState == DiagnosticStageState.Started);
            diagnosticsSession.Events.ShouldContain(e =>
                e.Name == stageName &&
                e.Type == DiagnosticsEventType.EndStage &&
                e.StageState == DiagnosticStageState.Succeeded);
        }
    }

    [Fact]
    public async Task BuildAsync_StageThrows_PublishFailedLifecycleDiagnosticAndRethrow()
    {
        const string html = "<p>ignored by unit test</p>";
        const string failureMessage = "Style computation failed.";

        var document = new Mock<IDocument>().Object;
        _domProvider.Setup(x => x.LoadAsync(html, It.IsAny<LayoutOptions>())).ReturnsAsync(document);
        _styleComputer.Setup(x => x.Compute(document, It.IsAny<DiagnosticsSession?>()))
            .Throws(new InvalidOperationException(failureMessage));

        var diagnosticsSession = new DiagnosticsSession();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _builder.BuildAsync(html, new LayoutOptions(), diagnosticsSession));

        exception.Message.ShouldBe(failureMessage);
        diagnosticsSession.Events.ShouldContain(e =>
            e.Name == "stage/style" &&
            e.Type == DiagnosticsEventType.StartStage &&
            e.StageState == DiagnosticStageState.Started);
        diagnosticsSession.Events.ShouldContain(e =>
            e.Name == "stage/style" &&
            e.Type == DiagnosticsEventType.Error &&
            e.StageState == DiagnosticStageState.Failed &&
            e.Description == failureMessage);
        diagnosticsSession.Events.ShouldNotContain(e =>
            e.Name == "stage/style" &&
            e.Type == DiagnosticsEventType.EndStage &&
            e.StageState == DiagnosticStageState.Succeeded);
    }

    private void SetupSuccessfulPipeline(
        string html,
        IDocument document,
        BlockBox displayRoot,
        StyleTree styleTree,
        BoxTree boxTree,
        FragmentTree fragmentTree)
    {
        _domProvider.Setup(x => x.LoadAsync(html, It.IsAny<LayoutOptions>())).ReturnsAsync(document);
        _styleComputer.Setup(x => x.Compute(document, It.IsAny<DiagnosticsSession?>())).Returns(styleTree);
        _boxTreeBuilder.Setup(x => x.BuildDisplayTree(styleTree, It.IsAny<DiagnosticsSession?>()))
            .Returns(displayRoot);
        _boxTreeBuilder.Setup(x => x.BuildLayoutGeometry(displayRoot, styleTree, It.IsAny<DiagnosticsSession?>(), It.IsAny<BoxTreeBuildContext?>()))
            .Returns(boxTree);
        _fragmentBuilder.Setup(x => x.Build(boxTree, It.IsAny<FragmentBuildContext>()))
            .Returns(fragmentTree);
    }
}


