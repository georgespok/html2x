namespace Html2x.LayoutEngine.Contracts.Published;

/// <summary>
///     Defines the published output seam of the Layout Geometry stage.
/// </summary>
/// <remarks>
///     This seam sits between Layout Geometry and fragment tree building. It is intended
///     to carry only facts that are stable after layout completes: geometry, inline
///     layout, display facts, image facts, rule facts, table facts, and child order.
///     Core geometry values, published facts, and geometry guards belong to Layout
///     Geometry. Mutable boxes, box tree construction, and box-to-published adapters
///     belong to the layout implementation behind this seam.
///     Published layout output must not expose mutable box nodes, parser DOM objects,
///     or partial measurement state. Layout algorithms may mutate internal boxes while
///     resolving geometry, but callers past this seam should consume immutable
///     published facts instead of relying on hidden mutation order.
///     Invariants:
///     <list type="bullet">
///         <item>
///             <description>Every published block carries final used geometry.</description>
///         </item>
///         <item>
///             <description>Children are already in fragment tree source order.</description>
///         </item>
///         <item>
///             <description>Block flow preserves the relative order of inline segments and child blocks.</description>
///         </item>
///         <item>
///             <description>Inline layout is either absent or complete for the owning block.</description>
///         </item>
///         <item>
///             <description>Image facts appear only for published blocks that represent images.</description>
///         </item>
///         <item>
///             <description>Rule facts appear only for published blocks that represent horizontal rules.</description>
///         </item>
///         <item>
///             <description>Table facts appear only for published blocks that represent tables, rows, or cells.</description>
///         </item>
///         <item>
///             <description>Published types do not expose mutable box nodes or parser DOM objects.</description>
///         </item>
///     </list>
/// </remarks>
internal sealed record PublishedLayoutTree
{
    public PublishedLayoutTree(
        PublishedPage page,
        IReadOnlyList<PublishedBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(page);

        Page = page;
        Blocks = PublishedLayoutGuard.CopyList(blocks, nameof(blocks));
    }

    public PublishedPage Page { get; }

    public IReadOnlyList<PublishedBlock> Blocks { get; }
}
