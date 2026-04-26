namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Documents which child collection a fragment descriptor must translate with the fragment.
/// </summary>
internal enum FragmentChildTraversalPolicy
{
    None,
    Children,
    Rows,
    Cells
}
