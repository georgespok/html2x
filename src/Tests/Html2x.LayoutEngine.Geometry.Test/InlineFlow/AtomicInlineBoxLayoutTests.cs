using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Resources;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Html2x.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.InlineFlow;

public sealed class AtomicInlineBoxLayoutTests
{
    [Fact]
    public void MeasureInlineBlock_ImageContent_UsesImageSizingWithoutTextLineMeasurement()
    {
        var textMeasurer = new CountingTextMeasurer();
        var imageSizingRules = new ImageSizingRules(new()
        {
            ImageMetadataResolver = new FixedImageMetadataResolver(new(32d, 16d))
        });
        var inlineBlock = InlineBlockBoxTree.Create();
        InlineBlockBoxTree.AddImage(inlineBlock, "image.png", new()
        {
            WidthPt = 32f,
            HeightPt = 16f,
            Padding = new(2f, 2f, 2f, 2f)
        });
        var layout = CreateLayout(textMeasurer, imageSizingRules);

        var result = layout.MeasureInlineBlock(inlineBlock, 100f);

        result.ShouldNotBeNull();
        var imageResolution = result.ImageResolution.ShouldNotBeNull();
        result.ContentWidth.ShouldBe(32f);
        result.ContentHeight.ShouldBe(16f);
        result.BorderBoxWidth.ShouldBe(36f);
        result.BorderBoxHeight.ShouldBe(20f);
        imageResolution.Src.ShouldBe("image.png");
        imageResolution.IntrinsicSizePx.ShouldBe(new SizePx(32d, 16d));
        imageResolution.Status.ShouldBe(ImageLoadStatus.Ok);
        result.TextLayout.Lines.ShouldBeEmpty();
        textMeasurer.MeasuredTexts.ShouldBe([""]);
    }

    [Fact]
    public void MeasureInlineBlock_TextContent_UsesInlineBoxSizingWithoutMutatingContentBox()
    {
        var textMeasurer = new CountingTextMeasurer();
        var inlineBlock = InlineBlockBoxTree.Create();
        var contentBox = InlineBlockBoxTree.AddContentBox(inlineBlock, new()
        {
            WidthPt = 42f,
            HeightPt = 18f,
            Padding = new(2f, 3f, 4f, 5f),
            Borders = BorderEdges.Uniform(new(1f, ColorRgba.Black, BorderLineStyle.Solid))
        });
        InlineBlockBoxTree.AddInline(contentBox, "alpha beta");
        var layout = CreateLayout(textMeasurer);

        var result = layout.MeasureInlineBlock(inlineBlock, 100f);

        result.ShouldNotBeNull();
        result.ContentBox.ShouldBeSameAs(contentBox);
        result.ContentWidth.ShouldBe(42f);
        result.ContentHeight.ShouldBe(18f);
        result.BorderBoxWidth.ShouldBe(52f);
        result.BorderBoxHeight.ShouldBe(26f);
        result.TextLayout.Lines.ShouldNotBeEmpty();
        textMeasurer.MeasureCount.ShouldBeGreaterThan(0);
        contentBox.UsedGeometry.ShouldBeNull();
        contentBox.InlineLayout.ShouldBeNull();
    }

    [Fact]
    public void MeasureInlineBlock_BlockDescendants_DoNotMutateBlocks()
    {
        var textMeasurer = new CountingTextMeasurer();
        var inlineBlock = InlineBlockBoxTree.Create();
        var contentBox = InlineBlockBoxTree.AddContentBox(inlineBlock, new()
        {
            WidthPt = 42f
        });
        InlineBlockBoxTree.AddInline(contentBox, "alpha");
        var nestedBlock = InlineBlockBoxTree.AddBlock(contentBox, new()
        {
            HeightPt = 30f,
            WidthPt = 20f
        });
        var layout = CreateLayout(textMeasurer);

        var result = layout.MeasureInlineBlock(inlineBlock, 100f);

        result.ShouldNotBeNull();
        result.ContentHeight.ShouldBe(30f);
        result.TextLayout.Lines.ShouldNotBeEmpty();
        contentBox.UsedGeometry.ShouldBeNull();
        contentBox.InlineLayout.ShouldBeNull();
        nestedBlock.UsedGeometry.ShouldBeNull();
        nestedBlock.InlineLayout.ShouldBeNull();
    }

    private static AtomicInlineBoxLayout CreateLayout(
        ITextMeasurer textMeasurer,
        ImageSizingRules? imageSizingRules = null) =>
        new(
            textMeasurer,
            new DefaultFontMetricsMeasurer(),
            new LineHeightRules(),
            new(),
            imageSizingRules ?? new ImageSizingRules());

    private sealed class FixedImageMetadataResolver(SizePx intrinsicSize) : IImageMetadataResolver
    {
        public ImageMetadataResult Resolve(string src) =>
            new()
            {
                Src = src,
                Status = ImageLoadStatus.Ok,
                IntrinsicSizePx = intrinsicSize
            };
    }

    private sealed class CountingTextMeasurer : ITextMeasurer
    {
        public int MeasureCount { get; private set; }

        public List<string> MeasuredTexts { get; } = [];

        public TextMeasurement Measure(FontKey font, float sizePt, string text)
        {
            MeasureCount++;
            MeasuredTexts.Add(text);
            return new(
                MeasureWidth(text),
                9f,
                3f,
                new(
                    font.Family,
                    font.Weight,
                    font.Style,
                    "test://font"));
        }

        private static float MeasureWidth(string text) => text.Length;
    }
}
