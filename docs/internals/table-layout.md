# Table Layout

This document explains Html2x table behavior for developers maintaining layout, diagnostics, and PDF rendering.

## Current Model

The table implementation supports deterministic rectangular tables without row or column spans. Unsupported table structures must preserve surrounding document flow and emit diagnostics instead of producing partial grids.

## Supported Behavior

- Fixed-width tables render as a shared grid.
- The widest row defines the derived column count.
- Columns split the resolved table width evenly when no per-column widths are provided.
- Table, row, and cell borders render in PDF output.
- Cell padding is preserved.
- Cell content is top-aligned.
- Header cells remain identifiable in fragments and diagnostics snapshots.
- Row backgrounds render behind the full row.
- Cell backgrounds override row backgrounds inside cell bounds.

## Unsupported Behavior

- `colspan`.
- `rowspan`.
- Non-rectangular table structures.
- Complex browser table layout behavior.

Unsupported tables should emit diagnostics and avoid rendering an incorrect visible grid.

## Pipeline

```text
HTML table
  -> TableGridLayout derives rows, columns, widths, and support status
  -> TableBlockLayoutRule places table behavior inside BlockLayoutEngine dispatch
  -> LayoutBoxStateWriter materializes table, row, and cell boxes
  -> FragmentBuilder emits table fragments
  -> LayoutSnapshotMapper records table structure
  -> PDF renderer paints backgrounds, borders, and cell content
```

## Diagnostics

Supported and unsupported table decisions use:

- `layout/table`
- `layout/table/unsupported-structure`

Payloads should include source path, row count, derived column count, requested width, resolved width, outcome, and rejection reason when applicable.

## Tests

Table changes should include layout tests, fragment tests, diagnostics tests, and focused renderer coverage when visual output changes.
