using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Diagnostics;

public sealed class FragmentSnapshot
{
    public string Kind { get; init; } = string.Empty;

    public float X { get; init; }

    public float Y { get; init; }

    public SizePt Size { get; init; }

    public string? Text { get; init; }

    public float? ContentX { get; init; }

    public float? ContentY { get; init; }

    public SizePt? ContentSize { get; init; }

    public BorderEdges? Borders { get; init; }

    public IReadOnlyList<FragmentSnapshot> Children { get; init; } = [];
}
