# Basic Table Structure

## Purpose

This feature adds baseline table rendering for simple HTML tables in Html2x. It is intentionally narrow. The goal is deterministic output for fixed-width, non-spanning tables that can be diagnosed and extended later without undoing the current layout pipeline.

## Supported Behavior

- Fixed-width tables render as a shared grid.
- The widest row defines the derived column count.
- Columns split the resolved table width evenly when no per-column widths are provided.
- Table borders and cell borders render in PDF output.
- Cell padding is preserved.
- Cell content stays top-aligned.
- Header cells (`th`) remain identifiable through layout snapshots and fragments.
- Row backgrounds render behind the full row.
- Cell backgrounds override row backgrounds inside the cell bounds.
- Tables appear in layout snapshots as `table`, `table-row`, and `table-cell` fragments.

## Unsupported Behavior

- Any `colspan` is unsupported.
- Any `rowspan` is unsupported.
- Unsupported tables do not render a visible grid.
- Unsupported tables still emit diagnostics and preserve surrounding document flow.

## Diagnostics

Supported tables emit:

- `layout/table`

The payload reports:

- source node path
- row count
- derived column count
- requested width
- resolved width
- outcome

Unsupported tables emit:

- `layout/table/unsupported-structure`
- `layout/table`

The unsupported trace keeps the same table context but marks `Outcome` as `Unsupported` and includes the rejection reason.

## Pipeline

```text
table -> layout grid -> fragments -> PDF drawing
```

In practice:

```text
HTML table
  -> TableLayoutEngine derives rows, columns, widths, and support status
  -> BlockLayoutEngine materializes table/row/cell boxes
  -> FragmentBuilder emits table-specific fragments
  -> LayoutSnapshotMapper records table structure for diagnostics
  -> SkiaFragmentDrawer paints backgrounds, borders, and cell content
```

## Manual Verification

Sample file:

- `C:\Projects\html2x\src\Tests\Html2x.TestConsole\html\basic-table-structure.html`

Generate the PDF:

Run from `C:\Projects\html2x\src\Tests\Html2x.TestConsole` so the console harness can resolve its local `.\fonts` directory.

```powershell
dotnet run --no-launch-profile --project C:\Projects\html2x\src\Tests\Html2x.TestConsole\Html2x.TestConsole.csproj -- C:\Projects\html2x\src\Tests\Html2x.TestConsole\html\basic-table-structure.html C:\Projects\html2x\build\basic-table-structure.pdf
```

Review expectations:

- Example 1 shows a stable 2x2 grid with equal columns.
- Example 2 shows header styling, row background layering, and cell background override.
- The unsupported merged-cell example should not render a table grid and should produce diagnostics instead.

## Failure Modes

- If the table mixes unsupported structure such as `colspan` or `rowspan`, the table is rejected and reported through diagnostics.
- If a table contains unsupported nesting patterns, the layout result is also rejected deterministically.
- If later content disappears after an unsupported table, inspect the `LayoutBuild` snapshot and `layout/table/unsupported-structure` event first.

## So What

This feature establishes a stable baseline for tables without pretending to support the full HTML table model. That keeps current behavior predictable and leaves room for later work on captions, authored column widths, and merged cells.
