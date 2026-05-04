# Geometry

Geometry must be computed once during layout, then projected forward without reinterpretation.

`Html2x.LayoutEngine.Geometry` consumes parser-free `StyleTree` input from
`Html2x.LayoutEngine.Contracts.Style`. It may read `ComputedStyle`,
`StyledElementFacts`, and ordered `StyleContentNode` values. It must not parse
HTML or CSS, reference AngleSharp, or traverse DOM nodes.

## Authority

`UsedGeometry` is the canonical geometry value for block-level layout nodes. It carries:

- Border box rectangle.
- Content box rectangle.
- Baseline when available.
- Marker offset.
- Overflow allowance.

`BoxGeometryFactory` owns geometry construction and normalization at layout boundaries. It handles finite-value normalization, non-negative sizes, content rectangle calculation, marker offsets, padding, and borders.

## Helper Ownership

Point-space geometry is owned by `Html2x.RenderModel` through `RectPt`,
`PointPt`, and `SizePt`. Pagination uses `RectPt.Translate` and
`PointPt.Translate` when translating cloned render model rectangles and text
run origins to page-local coordinates.

`UsedGeometry` translation remains geometry-owned in
`Html2x.LayoutEngine.Geometry`. Geometry may translate `UsedGeometry` because it
has to preserve geometry invariants through `BoxGeometryFactory`.
Production geometry placement should route `UsedGeometry` translation through
`GeometryTranslator`; direct `UsedGeometry` transformation helpers are
compatibility conveniences, not the place for new layout behavior.

Page content area calculation is a layout-owned fact in
`Html2x.LayoutEngine.Contracts`. `PageContentArea` is shared by geometry and
pagination so page margin normalization has one implementation.

Naming:

- `BorderBoxRect` is the painted border box.
- `ContentBoxRect` is the raw content box after padding and border are removed.
- `ContentFlowArea` is the marker-adjusted content area used for child block and inline flow placement.
- Pagination placement `PageX` and `PageY` are translated page coordinates derived from the placed fragment.

## Flow

```text
Style tree
  -> initial box tree from StyledElementFacts and StyleContentNode
  -> layout computes UsedGeometry
  -> published layout tree
  -> fragments copy published geometry
  -> pagination translates fragment coordinates
  -> renderer draws fragment rectangles
```

Fragments, pagination, diagnostics, and renderers must not recompute padding,
border, content rectangles, marker offsets, or block sizes. They may translate
already-published rectangles when pagination moves a fragment subtree to another
page.

## Block Flow Locality

`BlockLayoutEngine` orchestrates block kind dispatch and publication. Block-flow
locality lives in smaller internal modules:

- `BlockFlowLayoutExecutor` owns laid-out block-flow sequencing: cursor state,
  margin collapse, inline flushing, child ordering, and flow item ordering.
- `BlockFlowMeasurementExecutor` owns non-mutating stacked block measurement so
  layout and measurement share the same block-flow policy.
- `ImageBlockLayoutApplier` applies image metadata and image block geometry.
- `TableBlockLayoutApplier` owns table diagnostics and table placement.
- `PublishedLayoutPublisher` owns published block caching, source order, and
  inline publishing.
- `BlockLayoutState` is the helper that writes shared mutable block
  compatibility state for normal block layout.

## Compatibility Fields

`BlockBox` layout facts such as margin, padding, inline layout, used geometry, and inline-block context are internal mutable compatibility state. New downstream code should consume `PublishedLayoutTree` or render model fragments, not these fields.

Table layout may still expose scalar row and cell metadata for fragment projection. New code should prefer `UsedGeometry` and published table facts.

## Validation Policy

Layout construction is forgiving at the boundary where raw layout calculations enter `BoxGeometryFactory`. It may clamp or normalize invalid intermediate values.

Published geometry is strict. `UsedGeometry` and renderable fragments should reject non-finite coordinates and negative sizes so invalid geometry fails close to the producing stage.

## Invariants

- Every laid-out block has `UsedGeometry`.
- Mutable layout geometry is published from `UsedGeometry`.
- Fragment rectangles come from layout geometry.
- Pagination preserves width and height.
- Pagination translation preserves nested baselines, text origins, image content rectangles, line occupied rectangles, and block-like metadata.

## Diagnostics

When diagnostics are enabled, `layout/geometry-snapshot` captures box geometry, fragment geometry, and pagination placements. Use it to investigate drift between layout, fragment projection, and pagination.
