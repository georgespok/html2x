using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

public class InlineRunFactoryTests
{
    [Fact]
    public void TryBuildInlineBlockRun_ShouldNotFlattenInlineBlockToText()
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        var inlineBlock = new InlineBox(DisplayRole.InlineBlock)
        {
            Style = style
        };

        var contentBlock = new BlockBox(DisplayRole.Block)
        {
            Style = style,
            Parent = inlineBlock,
            IsAnonymous = true
        };
        contentBlock.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Style = style,
            TextContent = "Inline-block A",
            Parent = contentBlock
        });
        inlineBlock.Children.Add(contentBlock);

        var factory = new InlineRunFactory(new FakeMetricsProvider());

        factory.TryBuildInlineBlockRun(inlineBlock, 1, out _).ShouldBeFalse();
    }

    private sealed class FakeMetricsProvider : IFontMetricsProvider
    {
        public FontKey GetFontKey(ComputedStyle style) => new("Test", FontWeight.W400, FontStyle.Normal);

        public float GetFontSize(ComputedStyle style) => style.FontSizePt;

        public (float ascent, float descent) GetMetrics(FontKey font, float sizePt) => (8f, 2f);

        public float MeasureTextWidth(FontKey font, float sizePt, string text) => text.Length * sizePt;
    }
}
