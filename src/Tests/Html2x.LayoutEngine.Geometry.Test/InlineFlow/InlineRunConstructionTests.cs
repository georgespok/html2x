using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.InlineFlow;

public class InlineRunConstructionTests
{
    [Fact]
    public void BuildInlineBlockRun_DoesNotFlattenInlineBlockToText()
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        var inlineBlock = InlineBlockBoxTree.Create(style);
        var contentBlock = InlineBlockBoxTree.AddContentBox(inlineBlock, style, isAnonymous: true);
        InlineBlockBoxTree.AddInline(contentBlock, "Inline-block A", style);

        var factory = new InlineRunConstruction(new FakeMetricsMeasurer());

        factory.BuildInlineBlockRun(inlineBlock, 1, null).ShouldBeNull();
    }

    private sealed class FakeMetricsMeasurer : IFontMetricsMeasurer
    {
        public FontKey GetFontKey(ComputedStyle style) => new("Test", FontWeight.W400, FontStyle.Normal);

        public float GetFontSize(ComputedStyle style) => style.FontSizePt;

        public (float ascent, float descent) GetMetrics(FontKey font, float sizePt) => (8f, 2f);

        public float MeasureTextWidth(FontKey font, float sizePt, string text) => text.Length * sizePt;
    }
}
