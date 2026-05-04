using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Style;
using Shouldly;

namespace Html2x.LayoutEngine.Style.Test.Assertions;


internal static class StyleSnapshotAssertions
{
    public static void ShouldMatch(this StyleSnapshot actual, StyleSnapshot expected)
    {
        actual.Tag.ShouldBe(expected.Tag, $"Tag mismatch at <{expected.Tag}>");

        if (expected.Style is not null)
        {
            AssertComputedStyle(actual.Style, expected.Style);
        }

        if (!(expected.Children?.Count > 0))
        {
            return;
        }

        actual.Children?.Count.ShouldBe(expected.Children.Count,
            $"Expected {expected.Children.Count} children for <{expected.Tag}> but found {actual.Children.Count}");

        for (var i = 0; i < expected.Children.Count; i++)
        {
            actual.Children?[i].ShouldMatch(expected.Children[i]);
        }
    }

    private static void AssertComputedStyle(ComputedStyle? actual, ComputedStyle expected)
    {
        if (actual is null)
        {
            throw new Xunit.Sdk.XunitException("Actual style is null");
        }

        actual.FontFamily.ShouldBe(expected.FontFamily, "FontFamily mismatch");
        actual.FontSizePt.ShouldBe(expected.FontSizePt, "FontSizePt mismatch");
        actual.TextAlign.ShouldBe(expected.TextAlign, "TextAlign mismatch");
        actual.FloatDirection.ShouldBe(expected.FloatDirection, "FloatDirection mismatch");
        actual.Position.ShouldBe(expected.Position, "Position mismatch");
        actual.Color.ShouldBe(expected.Color, "Color mismatch");
        actual.Borders.ShouldBeEquivalentTo(expected.Borders, "Borders mismatch");

        if (expected.Bold)
        {
            actual.Bold.ShouldBeTrue("Bold mismatch");
        }

        if (expected.Italic)
        {
            actual.Italic.ShouldBeTrue("Italic mismatch");
        }

        if (expected.Decorations != TextDecorations.None)
        {
            actual.Decorations.ShouldBe(expected.Decorations, "Decorations mismatch");
        }

        if (expected.LineHeightMultiplier != 0f)
        {
            actual.LineHeightMultiplier.ShouldBe(expected.LineHeightMultiplier, "LineHeightMultiplier mismatch");
        }

        if (expected.BackgroundColor is not null)
        {
            actual.BackgroundColor.ShouldBe(expected.BackgroundColor, "BackgroundColor mismatch");
        }

        if (!string.IsNullOrEmpty(expected.Display))
        {
            actual.Display.ShouldBe(expected.Display, "Display mismatch");
        }

        if (expected.Margin != default)
        {
            actual.Margin.ShouldBe(expected.Margin, "Margin mismatch");
        }

        if (expected.Padding != default)
        {
            actual.Padding.ShouldBe(expected.Padding, "Padding mismatch");
        }

        if (expected.WidthPt.HasValue)
        {
            actual.WidthPt.ShouldBe(expected.WidthPt, "WidthPt mismatch");
        }

        if (expected.MinWidthPt.HasValue)
        {
            actual.MinWidthPt.ShouldBe(expected.MinWidthPt, "MinWidthPt mismatch");
        }

        if (expected.MaxWidthPt.HasValue)
        {
            actual.MaxWidthPt.ShouldBe(expected.MaxWidthPt, "MaxWidthPt mismatch");
        }

        if (expected.HeightPt.HasValue)
        {
            actual.HeightPt.ShouldBe(expected.HeightPt, "HeightPt mismatch");
        }

        if (expected.MinHeightPt.HasValue)
        {
            actual.MinHeightPt.ShouldBe(expected.MinHeightPt, "MinHeightPt mismatch");
        }

        if (expected.MaxHeightPt.HasValue)
        {
            actual.MaxHeightPt.ShouldBe(expected.MaxHeightPt, "MaxHeightPt mismatch");
        }
    }
}
