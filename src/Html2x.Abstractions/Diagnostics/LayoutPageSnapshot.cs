namespace Html2x.Abstractions.Diagnostics;

public sealed class LayoutPageSnapshot
{
    public int PageNumber { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }

    public float MarginTop { get; init; }

    public float MarginRight { get; init; }

    public float MarginBottom { get; init; }

    public float MarginLeft { get; init; }

    public IReadOnlyList<FragmentSnapshot> Fragments { get; init; } = [];
}