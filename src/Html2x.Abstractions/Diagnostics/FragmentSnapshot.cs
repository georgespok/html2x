using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Diagnostics;

public sealed class FragmentSnapshot
{
    public int SequenceId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public float X { get; init; }

    public float Y { get; init; }

    public SizePt Size { get; init; }

    public ColorRgba? Color { get; init; }

    public ColorRgba? BackgroundColor { get; init; }

    public Spacing? Margin { get; init; }

    public Spacing? Padding { get; init; }

    public float? WidthPt { get; init; }

    public float? HeightPt { get; init; }

    public string? Display { get; init; }

    public string? Text { get; init; }

    public float? ContentX { get; init; }

    public float? ContentY { get; init; }

    public SizePt? ContentSize { get; init; }

    public BorderEdges? Borders { get; init; }

    public FragmentDisplayRole? DisplayRole { get; init; }

    public FormattingContextKind? FormattingContext { get; init; }

    public float? MarkerOffset { get; init; }

    public int? DerivedColumnCount { get; init; }

    public int? RowIndex { get; init; }

    public int? ColumnIndex { get; init; }

    public bool? IsHeader { get; init; }

    public IReadOnlyList<FragmentSnapshot> Children { get; init; } = [];
}
