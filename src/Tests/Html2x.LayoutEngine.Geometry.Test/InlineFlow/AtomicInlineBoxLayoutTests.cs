using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
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
        var imageResolver = new ImageSizingRules(new()
        {
            ImageMetadataResolver = new FixedImageMetadataResolver(new(32d, 16d))
        });
        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Style = new()
        };
        inlineBlock.Children.Add(new ImageBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            Src = "image.png",
            Style = new()
            {
                WidthPt = 32f,
                HeightPt = 16f,
                Padding = new(2f, 2f, 2f, 2f)
            }
        });
        var layout = new AtomicInlineBoxLayout(
            textMeasurer,
            new FontMetricsProvider(),
            new DefaultLineHeightStrategy(),
            new(),
            imageResolver);

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
        result.Layout.Lines.ShouldBeEmpty();
        textMeasurer.MeasureCount.ShouldBe(0);
        textMeasurer.MeasureWidthCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureInlineBlock_TextContent_UsesInlineBoxSizingWithoutMutatingContentBox()
    {
        var textMeasurer = new CountingTextMeasurer();
        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Style = new()
        };
        var contentBox = new BlockBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            IsInlineBlockContext = true,
            Style = new()
            {
                WidthPt = 42f,
                HeightPt = 18f,
                Padding = new(2f, 3f, 4f, 5f),
                Borders = BorderEdges.Uniform(new(1f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        contentBox.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = contentBox,
            Style = contentBox.Style,
            TextContent = "alpha beta"
        });
        inlineBlock.Children.Add(contentBox);
        var layout = new AtomicInlineBoxLayout(
            textMeasurer,
            new FontMetricsProvider(),
            new DefaultLineHeightStrategy(),
            new(),
            new ImageSizingRules());

        var result = layout.MeasureInlineBlock(inlineBlock, 100f);

        result.ShouldNotBeNull();
        result.ContentBox.ShouldBeSameAs(contentBox);
        result.ContentWidth.ShouldBe(42f);
        result.ContentHeight.ShouldBe(18f);
        result.BorderBoxWidth.ShouldBe(52f);
        result.BorderBoxHeight.ShouldBe(26f);
        result.Layout.Lines.ShouldNotBeEmpty();
        textMeasurer.MeasureCount.ShouldBeGreaterThan(0);
        contentBox.UsedGeometry.ShouldBeNull();
        contentBox.InlineLayout.ShouldBeNull();
    }

    private sealed class FixedImageMetadataResolver(SizePx intrinsicSize) : IImageMetadataResolver
    {
        public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes) =>
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

        public int MeasureWidthCount { get; private set; }

        public TextMeasurement Measure(FontKey font, float sizePt, string text)
        {
            MeasureCount++;
            return new(
                MeasureWidth(font, sizePt, text),
                9f,
                3f,
                new(
                    font.Family,
                    font.Weight,
                    font.Style,
                    "test://font"));
        }

        public float MeasureWidth(FontKey font, float sizePt, string text)
        {
            MeasureWidthCount++;
            return text.Length;
        }

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) => (9f, 3f);
    }
}
