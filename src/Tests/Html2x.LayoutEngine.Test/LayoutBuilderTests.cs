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

using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Test;

public class LayoutBuilderTests
{
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
    public async Task Build_ShouldOrchestratePipelineAndReturnLayout()
    {
        // Arrange
        const string html = "<p>ignored by unit test</p>";

        var document = new Mock<IDocument>().Object;
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

        _domProvider.Setup(x => x.LoadAsync(html, It.IsAny<LayoutOptions>())).ReturnsAsync(document);
        _styleComputer.Setup(x => x.Compute(document)).Returns(styleTree);
        _boxTreeBuilder.Setup(x => x.Build(styleTree, It.IsAny<DiagnosticsSession?>())).Returns(boxTree);
        _fragmentBuilder.Setup(x => x.Build(boxTree, It.IsAny<FragmentBuildContext>()))
            .Returns(fragmentTree);

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
        layout.Pages[0].Children[0].ShouldBeSameAs(expectedBlock);

        _domProvider.Verify(x => x.LoadAsync(html, options), Times.Once);
        _styleComputer.Verify(x => x.Compute(document), Times.Once);
        _boxTreeBuilder.Verify(x => x.Build(styleTree, It.IsAny<DiagnosticsSession?>()), Times.Once);
        _fragmentBuilder.Verify(x => x.Build(boxTree, It.IsAny<FragmentBuildContext>()), Times.Once);
    }

    
}


