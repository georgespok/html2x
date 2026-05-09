# Table Layout

This page explains table behavior across style, geometry, diagnostics, fragment
projection, pagination, and PDF rendering.

## Current Model

The table implementation supports deterministic tables with horizontal column spans. Unsupported table structures must preserve surrounding document flow and emit diagnostics instead of producing partial grids.

## Data Flow

```text
HTML table
  -> StyleTree table nodes
  -> TableStructure
  -> TableGridLayout
  -> TableBlockLayout
  -> PublishedLayoutTree table facts
  -> FragmentTree table fragments
  -> pagination audit metadata
  -> PDF renderer table paint
```

## Supported Behavior

- Fixed-width tables render as a shared grid.
- The row with the widest effective span total defines the derived column count.
- Columns split the resolved table width evenly when no per-column widths are provided.
- `td` and `th` cells with positive integer `colspan` values occupy the sum of
  their spanned derived column widths.
- Table, row, and cell borders render in PDF output.
- Cell padding is preserved.
- Cell content is top-aligned.
- Header cells remain identifiable in fragments and diagnostics snapshots.
- Row backgrounds render behind the full row.
- Cell backgrounds override row backgrounds inside cell bounds.

## Unsupported Behavior

- `rowspan`.
- Non-rectangular table structures.
- Complex browser table layout behavior.

Unsupported tables should emit diagnostics and avoid rendering an incorrect visible grid.

## Ownership

`Html2x.LayoutEngine.Geometry.Tables` owns table structure, grid layout,
measurement, placement, and table-specific diagnostic vocabulary. Mutable table
models remain internal geometry state. `TableGridDiagnostics` emits diagnostic
records from the diagnostics owner.

Fragment projection copies published table facts into render model fragments.
Pagination treats table fragments as block-boundary content and preserves table
metadata in placement audit facts. The PDF renderer paints table backgrounds,
borders, and cell content from render model facts only.

## Diagnostics

Supported and unsupported table decisions use:

- `layout/table`
- `layout/table/unsupported-structure`

Payloads should include source path, row count, derived column count, requested width, resolved width, outcome, and rejection reason when applicable.
