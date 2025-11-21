namespace Html2x.Abstractions.Diagnostics;

public sealed class LayoutSnapshot
{
    public int PageCount { get; init; }

    public IReadOnlyList<LayoutPageSnapshot> Pages { get; init; } = [];
}