using Html2x.Core.Layout;

namespace Html2x.Layout.Fragment;

public sealed class FragmentTree
{
    /// <summary>
    ///     Top-level block fragments representing flow content of the page.
    /// </summary>
    public List<BlockFragment> Blocks { get; } = [];

    /// <summary>
    ///     Optional flattened list of all fragments (useful for rendering order).
    /// </summary>
    public List<Core.Layout.Fragment> All { get; } = [];

    /// <summary>
    ///     Optional page-level metadata (A4, margins, etc.)
    /// </summary>
    public LayoutPageMetadata Page { get; } = new();
}