using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Models;

public sealed record InlineLayoutResult(
    IReadOnlyList<InlineFlowSegmentLayout> Segments,
    float TotalHeight,
    float MaxLineWidth)
{
    public static InlineLayoutResult Empty { get; } = new([], 0f, 0f);
}

public sealed record InlineFlowSegmentLayout(
    IReadOnlyList<InlineLineLayout> Lines,
    float Top,
    float Height);

public sealed record InlineLineLayout(
    int LineIndex,
    RectangleF Rect,
    float BaselineY,
    float LineHeight,
    string? TextAlign,
    IReadOnlyList<InlineLineItemLayout> Items);

public abstract record InlineLineItemLayout(int Order, RectangleF Rect);

public sealed record InlineTextItemLayout(
    int Order,
    RectangleF Rect,
    IReadOnlyList<TextRun> Runs,
    IReadOnlyList<InlineBox> Sources)
    : InlineLineItemLayout(Order, Rect);

public sealed record InlineObjectItemLayout(
    int Order,
    RectangleF Rect,
    BlockBox ContentBox)
    : InlineLineItemLayout(Order, Rect);

public readonly record struct InlineLayoutRequest(
    float ContentLeft,
    float ContentTop,
    float AvailableWidth,
    bool IncludeSyntheticListMarker = true)
{
    public static InlineLayoutRequest ForMeasurement(float availableWidth, bool includeSyntheticListMarker = true)
    {
        return new InlineLayoutRequest(0f, 0f, availableWidth, includeSyntheticListMarker);
    }
}
