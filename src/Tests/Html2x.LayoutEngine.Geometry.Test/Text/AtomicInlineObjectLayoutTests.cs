using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Text;
using Html2x.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public sealed class AtomicInlineObjectLayoutTests
{
    [Fact]
    public void MeasureInlineBlock_ImageContent_UsesImageSizingWithoutTextLineMeasurement()
    {
        var textMeasurer = new CountingTextMeasurer();
        var imageResolution = new ImageLayoutResolution(
            "image.png",
            new(32d, 16d),
            new(32d, 16d),
            ImageLoadStatus.Ok,
            32f,
            16f,
            36f,
            20f);
        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Style = new()
        };
        inlineBlock.Children.Add(new ImageBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            Style = new()
        });
        var layout = new AtomicInlineObjectLayout(
            textMeasurer,
            new FontMetricsProvider(),
            new DefaultLineHeightStrategy(),
            new(),
            new FixedImageSizingRules(imageResolution));

        var result = layout.MeasureInlineBlock(inlineBlock, 100f);

        result.ShouldNotBeNull();
        result.ContentWidth.ShouldBe(32f);
        result.ContentHeight.ShouldBe(16f);
        result.BorderBoxWidth.ShouldBe(36f);
        result.BorderBoxHeight.ShouldBe(20f);
        result.ImageResolution.ShouldBe(imageResolution);
        result.Layout.Lines.ShouldBeEmpty();
        textMeasurer.MeasureCount.ShouldBe(0);
        textMeasurer.MeasureWidthCount.ShouldBe(0);
    }

    private sealed class FixedImageSizingRules(ImageLayoutResolution resolution) : IImageSizingRules
    {
        public ImageLayoutResolution Resolve(ImageBox imageBox, float availableWidth) => resolution;
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