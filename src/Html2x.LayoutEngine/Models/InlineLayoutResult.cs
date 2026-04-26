using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Models;

/// <summary>
/// Captures measured inline layout segments and aggregate dimensions for a block content area.
/// </summary>
public sealed record InlineLayoutResult(
    IReadOnlyList<InlineFlowSegmentLayout> Segments,
    float TotalHeight,
    float MaxLineWidth)
{
    public static InlineLayoutResult Empty { get; } = new([], 0f, 0f);
}

/// <summary>
/// Groups contiguous inline lines that share a coordinate space within a block.
/// </summary>
public sealed record InlineFlowSegmentLayout(
    IReadOnlyList<InlineLineLayout> Lines,
    float Top,
    float Height);

/// <summary>
/// Describes one inline line where Rect is the line slot and OccupiedRect is the tight item bounds.
/// </summary>
public sealed record InlineLineLayout(
    int LineIndex,
    RectangleF Rect,
    RectangleF OccupiedRect,
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
