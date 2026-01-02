using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Diagnostics;

public sealed class LayoutPageSnapshot
{
    public int PageNumber { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }

    public Spacing Margin { get; init; }

    public IReadOnlyList<FragmentSnapshot> Fragments { get; init; } = [];
}
