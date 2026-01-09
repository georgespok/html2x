using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentTree
{
    /// <summary>
    ///     Top-level block fragments representing flow content of the page.
    /// </summary>
    public List<BlockFragment> Blocks { get; } = [];

    /// <summary>
    ///     Optional flattened list of all fragments (useful for rendering order).
    /// </summary>
    public List<Abstractions.Layout.Fragments.Fragment> All { get; } = [];

}
