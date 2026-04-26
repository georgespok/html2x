# Geometry

Geometry must be computed once during layout, then projected forward without reinterpretation.

## Authority

`UsedGeometry` is the canonical geometry payload for block-level layout nodes. It carries:

- Border box rectangle.
- Content box rectangle.
- Baseline when available.
- Marker offset.
- Overflow allowance.

`BoxGeometryFactory` owns geometry construction and normalization at layout boundaries. It handles finite-value normalization, non-negative sizes, content rectangle calculation, marker offsets, padding, and borders.

## Flow

```text
Display tree
  -> layout computes UsedGeometry
  -> fragments copy geometry
  -> pagination translates fragment coordinates
  -> renderer draws fragment rectangles
```

Fragments, pagination, diagnostics, and renderers must not recompute padding, border, content rectangles, marker offsets, or block sizes.

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
