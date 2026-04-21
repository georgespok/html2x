# Geometry Contract

This note defines the target contract for layout geometry as Html2x transitions toward a single source of truth.

## Goal

Geometry must be computed once during layout and then projected forward without reinterpretation.

Target pipeline:

1. Box layout resolves used geometry.
2. Fragment building projects that geometry into renderer-facing fragments.
3. Pagination translates fragment coordinates per page.

Later stages may clone or translate geometry, but they must not invent new block rectangles.

## Production Contract

`Html2x.LayoutEngine.Models.UsedGeometry` is the canonical geometry payload for block-level layout nodes.

It carries:

- `BorderBoxRect`
- `ContentBoxRect`
- `Baseline`
- `MarkerOffset`
- `AllowsOverflow`

For the current migration window, `BlockBox.X`, `Y`, `Width`, `Height`, and `MarkerOffset` remain available as compatibility accessors. They mirror `UsedGeometry` and must not be written as an independent geometry source.

## Authority Rules

- `BlockLayoutEngine` owns block, table, row, and cell geometry.
- `TableLayoutEngine` returns row and cell placements using the same geometry vocabulary.
- Fragment adapters read geometry from `UsedGeometry`.
- Pagination may translate fragment coordinates, but it must preserve width and height.

Inline layout now follows the same rule for stored line and inline-object geometry:

- `InlineLayoutEngine` computes `InlineLayoutResult` once per owning block.
- `InlineFragmentStage` projects stored inline geometry and does not re-measure text.

## Internal Extension Seams

Production now keeps the extensibility seams internal while the contract stabilizes.

- `BlockLayoutStrategyRegistry` selects block-level layout strategies.
- `InlineNodeMeasurerRegistry` selects ordered inline measurers for atomic inline nodes.
- `FragmentAdapterRegistry` selects block and special fragment adapters.

Defaults preserve current behavior. New geometry-bearing node kinds can be added by prepending one strategy, one measurer when needed, and one adapter without editing the core stages.

Pagination remains a separate seam. If an adapter emits a new fragment runtime type instead of reusing the built-in block, table, line, image, or rule fragments, pagination also needs a matching translator registration in `FragmentCoordinateTranslator`.

## Guardrails

The test suite and diagnostics now make geometry drift visible before larger refactors land.

Expected invariants:

- Every laid out `BlockBox` carries `UsedGeometry`.
- Compatibility accessors match `UsedGeometry.BorderBoxRect`.
- Block and special fragments use `UsedGeometry`.
- Child block geometry stays inside the parent content box unless overflow is explicit.
- Pagination placements preserve fragment size and only translate coordinates when moving across pages.
- Pagination translation preserves nested line baselines, run origins, and image content rects.

Current limitation:

- Inline-block projection replays stored inline geometry. Nested block descendants inside inline-block content still need explicit projection coverage if they must materialize as child block fragments.

## Diagnostics

When diagnostics are enabled, `layout/geometry-snapshot` emits one payload with:

- box geometry from the box tree
- fragment geometry from the final `HtmlLayout`
- pagination placements from the paginator

This snapshot is intended for regression diffing and for explaining invariant failures without opening a debugger.

The mapper now assumes laid-out boxes already carry `UsedGeometry`. Missing geometry is treated as a contract violation instead of being reconstructed from compatibility fields.

## Migration Notes

This contract is intentionally incremental.

- Batch 1 established the geometry model, baseline tests, and diagnostics.
- Batch 2 moved inline layout onto stored `InlineLayoutResult`.
- Batch 3 moved images, rules, and table content onto the same contract.
- Batch 4 replaced the remaining hard-coded seams with internal registries and tightened invariant enforcement.

So what: production now has one geometry authority, internal seams for future node kinds, and a validator that fails as soon as projection or pagination drifts away from stored geometry.
