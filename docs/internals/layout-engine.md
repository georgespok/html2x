# Layout Engine

This page explains the internal composition layer. `Html2x.LayoutEngine`
coordinates style, geometry, fragment tree building, and pagination, but it does
not own the algorithms in those stages.

## Responsibilities

- Call the style module to parse HTML and CSS and produce `StyleTree`.
- Call the geometry module to produce `PublishedLayoutTree`.
- Project published layout into fragments.
- Paginate fragments into pages.
- Emit layout diagnostics when diagnostics are enabled.

## Related Modules

- `Html2x.LayoutEngine.Style` owns AngleSharp usage, user agent stylesheet
  application, CSS parsing, computed style construction, style diagnostics, and
  the parser-free `StyleTree` handoff.
- `Html2x.LayoutEngine.Geometry` consumes `StyleTree` and produces published
  layout geometry. Block flow delegates image placement, table placement, and
  published layout publishing to smaller internal modules.
- `Html2x.LayoutEngine.Pagination` consumes render model fragments and returns
  `PaginationResult` with final `HtmlLayout` plus pagination audit facts.
- `Html2x.Renderers.Pdf` consumes `HtmlLayout` only.

## Internal Entry Points

- `LayoutPipeline`
- `LayoutStageRunner`
- `StyleTreeBuilder`
- `LayoutGeometryConstruction`
- `FragmentTreeBuilder`
- `LayoutPaginator`

## Composition Boundary

`LayoutPipeline` constructs the concrete pipeline stages for the converter flow
and is reached through the public `HtmlConverter` facade. Its `BuildAsync`
method should read as the stage handoff sequence: style tree, published
geometry, fragments, pagination result, final layout.

`LayoutStageRunner` owns the diagnostics lifecycle wrapper for geometry,
fragment tree building, and pagination. Diagnostics observe stage execution, but
they do not define the composition flow. Layout-specific diagnostic payloads,
including the geometry snapshot, live in focused diagnostics modules.

The style stage is reached through `StyleTreeBuilder`. The geometry stage is
reached through `LayoutGeometryConstruction`.

The layout engine should keep style, geometry, fragment, and pagination
responsibilities separable even when implementation classes coordinate multiple
steps.

Inside geometry, keep `BlockBoxLayout` focused on block-flow orchestration.
Move specialized image, table, inline publishing, or shared mutable layout logic
behind focused internal modules when the logic can be tested through published
layout behavior.

When adding behavior:

1. Add style support first if the behavior comes from CSS.
2. Add geometry support if layout changes.
3. Add fragment fields only when renderers need the fact.
4. Add pagination translation support for new geometry-bearing fragment types.
5. Add diagnostics for unsupported or fallback behavior.

## Unsupported Modes

Unsupported future layout modes must remain explicit. Floats, absolute
positioning, and flexbox should emit diagnostics and use the documented fallback
until a real formatting context exists.

## See Also

- [Processing Pipeline](../architecture/pipeline.md)
- [Module Boundaries](../architecture/module-boundaries.md)
- [Style Engine](style-engine.md)
- [Geometry](geometry.md)
- [Fragment Tree Building](fragment-tree-building.md)
- [Pagination](pagination.md)
