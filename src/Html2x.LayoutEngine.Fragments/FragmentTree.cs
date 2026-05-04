using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Fragments;

internal sealed class FragmentTree
{
    /// <summary>
    ///     Top-level block fragments representing flow content of the page.
    /// </summary>
    public List<BlockFragment> Blocks { get; } = [];
}
