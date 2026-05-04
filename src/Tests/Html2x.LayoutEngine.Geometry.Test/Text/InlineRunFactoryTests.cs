using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Text;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public class InlineRunFactoryTests
{
    [Fact]
    public void TryBuildInlineBlockRun_DoesNotFlattenInlineBlockToText()
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Style = style
        };

        var contentBlock = new BlockBox(BoxRole.Block)
        {
            Style = style,
            Parent = inlineBlock,
            IsAnonymous = true
        };
        contentBlock.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Style = style,
            TextContent = "Inline-block A",
            Parent = contentBlock
        });
        inlineBlock.Children.Add(contentBlock);

        var factory = new InlineRunFactory(new FakeMetricsProvider());

        factory.TryBuildInlineBlockRun(inlineBlock, 1, inlineLayout: null, out _).ShouldBeFalse();
    }

    private sealed class FakeMetricsProvider : IFontMetricsProvider
    {
        public FontKey GetFontKey(ComputedStyle style) => new("Test", FontWeight.W400, FontStyle.Normal);

        public float GetFontSize(ComputedStyle style) => style.FontSizePt;

        public (float ascent, float descent) GetMetrics(FontKey font, float sizePt) => (8f, 2f);

        public float MeasureTextWidth(FontKey font, float sizePt, string text) => text.Length * sizePt;
    }
}
