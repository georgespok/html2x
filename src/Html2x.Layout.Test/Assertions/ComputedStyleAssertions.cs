using Html2x.Layout.Style;
using Shouldly;

namespace Html2x.Layout.Test.Assertions;

public sealed class ComputedStyleAssertions(ComputedStyle style)
{
    public ComputedStyleAssertions FontFamily(string family)
    {
        style.FontFamily.ShouldBe(family);
        return this;
    }

    public ComputedStyleAssertions FontSize(float sizePt, double tolerance = 0.01)
    {
        style.FontSizePt.ShouldBe(sizePt, tolerance);
        return this;
    }

    public ComputedStyleAssertions Bold(bool expected)
    {
        style.Bold.ShouldBe(expected);
        return this;
    }

    public ComputedStyleAssertions Italic(bool expected)
    {
        style.Italic.ShouldBe(expected);
        return this;
    }

    public ComputedStyleAssertions TextAlign(string align)
    {
        style.TextAlign.ShouldBe(align);
        return this;
    }

    public ComputedStyleAssertions Color(string color)
    {
        style.Color.ShouldBe(color);
        return this;
    }

    public ComputedStyleAssertions MarginTop(float value, double tolerance = 0.01)
    {
        style.MarginTopPt.ShouldBe(value, tolerance);
        return this;
    }

    public ComputedStyleAssertions MarginRight(float value, double tolerance = 0.01)
    {
        style.MarginRightPt.ShouldBe(value, tolerance);
        return this;
    }

    public ComputedStyleAssertions MarginBottom(float value, double tolerance = 0.01)
    {
        style.MarginBottomPt.ShouldBe(value, tolerance);
        return this;
    }

    public ComputedStyleAssertions MarginLeft(float value, double tolerance = 0.01)
    {
        style.MarginLeftPt.ShouldBe(value, tolerance);
        return this;
    }
}