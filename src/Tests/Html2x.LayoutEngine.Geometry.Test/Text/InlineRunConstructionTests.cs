using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public class InlineRunConstructionTests
{
    [Fact]
    public void BuildInlineBlockRun_DoesNotFlattenInlineBlockToText()
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

        var factory = new InlineRunConstruction(new FakeMetricsProvider());

        factory.BuildInlineBlockRun(inlineBlock, 1, null).ShouldBeNull();
    }

    private sealed class FakeMetricsProvider : IFontMetricsProvider
    {
        public FontKey GetFontKey(ComputedStyle style) => new("Test", FontWeight.W400, FontStyle.Normal);

        public float GetFontSize(ComputedStyle style) => style.FontSizePt;

        public (float ascent, float descent) GetMetrics(FontKey font, float sizePt) => (8f, 2f);

        public float MeasureTextWidth(FontKey font, float sizePt, string text) => text.Length * sizePt;
    }
}