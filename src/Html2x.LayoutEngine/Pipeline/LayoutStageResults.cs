using Html2x.Abstractions.Layout.Documents;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Pipeline;

/// <summary>
/// Captures the computed style tree after cascade and inheritance have completed.
/// </summary>
internal sealed record StyleStageResult(StyleTree Tree);

/// <summary>
/// Captures the display tree produced from the style stage.
/// </summary>
internal sealed record DisplayTreeStageResult(DisplayNode Root);

/// <summary>
/// Marks the box tree after layout geometry has been applied to layout-owned boxes.
/// </summary>
internal sealed record LayoutGeometryStageResult(BoxTree Tree);

/// <summary>
/// Captures renderable fragments projected from laid-out boxes.
/// </summary>
internal sealed record FragmentStageResult(FragmentTree Tree);

/// <summary>
/// Captures page placements produced from the fragment tree.
/// </summary>
internal sealed record PaginationStageResult(PaginationResult Result);

/// <summary>
/// Captures the final assembled layout consumed by renderers.
/// </summary>
internal sealed record LayoutAssemblyStageResult(HtmlLayout Layout);
