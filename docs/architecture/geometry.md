# Geometry

Geometry must be computed once during layout, then projected forward without reinterpretation.

`Html2x.LayoutEngine.Geometry` consumes parser-free `StyleTree` input from
`Html2x.LayoutEngine.Style`. It may read `ComputedStyle`, `StyledElementFacts`,
and ordered `StyleContentNode` values. It must not parse HTML or CSS, reference
AngleSharp, or traverse DOM nodes.

## Authority

`UsedGeometry` is the canonical geometry value for block-level layout nodes. It carries:

- Border box rectangle.
- Content box rectangle.
- Baseline when available.
- Marker offset.
- Overflow allowance.

`BoxGeometryFactory` owns geometry construction and normalization at layout boundaries. It handles finite-value normalization, non-negative sizes, content rectangle calculation, marker offsets, padding, and borders.

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

## Compatibility Fields

`BlockBox.X`, `Y`, `Width`, `Height`, and `MarkerOffset` remain compatibility accessors. They must mirror `UsedGeometry` and must not become a second geometry source.

Table layout may still expose scalar row and cell positions for compatibility. New code should prefer `UsedGeometry`.

## Validation Policy

Layout construction is forgiving at the boundary where raw layout calculations enter `BoxGeometryFactory`. It may clamp or normalize invalid intermediate values.

Published geometry is strict. `UsedGeometry` and renderable fragments should reject non-finite coordinates and negative sizes so invalid geometry fails close to the producing stage.

## Invariants

- Every laid-out block has `UsedGeometry`.
- Compatibility accessors match `UsedGeometry.BorderBoxRect`.
- Fragment rectangles come from layout geometry.
- Pagination preserves width and height.
- Pagination translation preserves nested baselines, text origins, image content rectangles, line occupied rectangles, and block-like metadata.

## Diagnostics

When diagnostics are enabled, `layout/geometry-snapshot` captures box geometry, fragment geometry, and pagination placements. Use it to investigate drift between layout, fragment projection, and pagination.
