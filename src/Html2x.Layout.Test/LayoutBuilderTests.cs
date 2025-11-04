using AngleSharp.Dom;
using Html2x.Core;
using Html2x.Core.Layout;
using Html2x.Layout.Box;
using Html2x.Layout.Dom;
using Html2x.Layout.Fragment;
using Html2x.Layout.Style;
using Moq;
using Shouldly;

namespace Html2x.Layout.Test;

public class LayoutBuilderTests
{
    private readonly Mock<IBoxTreeBuilder> _boxTreeBuilder;
    private readonly LayoutBuilder _builder;

    private readonly Mock<IDomProvider> _domProvider;
    private readonly Mock<IFragmentBuilder> _fragmentBuilder;
    private readonly Mock<IStyleComputer> _styleComputer;

    public LayoutBuilderTests()
    {
        _domProvider = new Mock<IDomProvider>();
        _styleComputer = new Mock<IStyleComputer>();
        _boxTreeBuilder = new Mock<IBoxTreeBuilder>();
        _fragmentBuilder = new Mock<IFragmentBuilder>();

        _builder = new LayoutBuilder(_domProvider.Object, _styleComputer.Object, _boxTreeBuilder.Object,
            _fragmentBuilder.Object);
    }

    [Fact]
    public async Task Build_ShouldOrchestratePipelineAndReturnLayout()
    {
        // Arrange
        const string html = "<p>ignored by unit test</p>";

        var document = new Mock<IDocument>().Object;
        var styleTree = new StyleTree
            { Page = { MarginTopPt = 10, MarginRightPt = 11, MarginBottomPt = 12, MarginLeftPt = 13 } };
        var boxTree = new BoxTree
        {
            Page =
            {
                MarginTopPt = 10,
                MarginRightPt = 11,
                MarginBottomPt = 12,
                MarginLeftPt = 13
            }
        };

        var expectedBlock = new BlockFragment();
        var fragmentTree = new FragmentTree();
        fragmentTree.Blocks.Add(expectedBlock);

        _domProvider.Setup(x => x.LoadAsync(html)).ReturnsAsync(document);
        _styleComputer.Setup(x => x.Compute(document)).Returns(styleTree);
        _boxTreeBuilder.Setup(x => x.Build(styleTree)).Returns(boxTree);
        _fragmentBuilder.Setup(x => x.Build(boxTree)).Returns(fragmentTree);

        // Act
        var layout = await _builder.BuildAsync(html, PaperSizes.Letter);

        // Assert
        Assert.NotNull(layout);
        Assert.IsType<HtmlLayout>(layout);
        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Margins.Top.ShouldBe(10);
        layout.Pages[0].Margins.Right.ShouldBe(11);
        layout.Pages[0].Margins.Bottom.ShouldBe(12);
        layout.Pages[0].Margins.Left.ShouldBe(13);
        layout.Pages[0].Children[0].ShouldBeSameAs(expectedBlock);

        _domProvider.Verify(x => x.LoadAsync(html), Times.Once);
        _styleComputer.Verify(x => x.Compute(document), Times.Once);
        _boxTreeBuilder.Verify(x => x.Build(styleTree), Times.Once);
        _fragmentBuilder.Verify(x => x.Build(boxTree), Times.Once);
    }
}