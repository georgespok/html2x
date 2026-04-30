using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Fragments;

public sealed class FragmentTree
{
    /// <summary>
    ///     Top-level block fragments representing flow content of the page.
    /// </summary>
    public List<BlockFragment> Blocks { get; } = [];
}
