using System.Globalization;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Text;
using Moq;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

public sealed class TextWrapperTests
{
    [Fact]
    public void Wrap_SplitsAtWhitespaceFirst()
    {
        var wrapper = new TextWrapper(CreateWidthPerGraphemeMeasurer(10f));
        var font = new FontKey("Inter", FontWeight.W400, FontStyle.Normal);

        var lines = wrapper.Wrap("alpha beta gamma", font, 12f, maxWidth: 50f);

        lines.Count.ShouldBe(3);
        lines[0].ShouldBe("alpha");
        lines[1].ShouldBe("beta");
        lines[2].ShouldBe("gamma");
    }

    [Fact]
    public void Wrap_FallsBackToCharacterSplitsForLongTokens()
    {
        var wrapper = new TextWrapper(CreateWidthPerGraphemeMeasurer(10f));
        var font = new FontKey("Inter", FontWeight.W400, FontStyle.Normal);

        var lines = wrapper.Wrap("abcdefghij", font, 12f, maxWidth: 30f);

        lines.Count.ShouldBe(4);
        lines[0].ShouldBe("abc");
        lines[1].ShouldBe("def");
        lines[2].ShouldBe("ghi");
        lines[3].ShouldBe("j");
    }

    [Fact]
    public void Wrap_IgnoresExtraWhitespace()
    {
        var wrapper = new TextWrapper(CreateWidthPerGraphemeMeasurer(10f));
        var font = new FontKey("Inter", FontWeight.W400, FontStyle.Normal);

        var lines = wrapper.Wrap("  alpha   beta  ", font, 12f, maxWidth: 100f);

        lines.Count.ShouldBe(2);
        lines[0].ShouldBe("alpha");
        lines[1].ShouldBe("beta");
    }

    [Fact]
    public void Wrap_SplitsOnExplicitNewlines()
    {
        var wrapper = new TextWrapper(CreateWidthPerGraphemeMeasurer(10f));
        var font = new FontKey("Inter", FontWeight.W400, FontStyle.Normal);

        var lines = wrapper.Wrap("alpha\nbeta", font, 12f, maxWidth: 200f);

        lines.Count.ShouldBe(2);
        lines[0].ShouldBe("alpha");
        lines[1].ShouldBe("beta");
    }

    private static ITextMeasurer CreateWidthPerGraphemeMeasurer(float widthPerGrapheme)
    {
        var mock = new Mock<ITextMeasurer>();
        mock.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey _, float _, string text) => CountGraphemes(text) * widthPerGrapheme);
        mock.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((0f, 0f));
        return mock.Object;
    }

    private static int CountGraphemes(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var count = 0;
        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }
}
